using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

// Yangi buyruq klassini AutoCAD ga ro'yxatdan o'tkazamiz (mavjud kodga tegmasdan).
[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.PolygonCommands))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// GPS nuqtalaridan (blok + tartib raqami atributi) berilgan raqamlar bo'yicha
    /// poligon (yopiq poliliniya) yasovchi buyruq.
    ///
    /// Foydalanish: PTPOLIGON -> raqamlarni kiriting, masalan:
    ///   145-165, 171, 182-260
    /// Shu raqamli nuqtalar (blok insert nuqtalari) ketma-ket tutashtirilib yopiq
    /// poliliniya chiziladi. Keyin uni PLTABLE / PLCHEGARA bilan ishlatish mumkin.
    /// </summary>
    public class PolygonCommands
    {
        [CommandMethod("PTPOLIGON")]
        public void PolygonFromPointNumbers()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1) Raqamlar ifodasini so'rash (probel bilan)
            var pso = new PromptStringOptions("\nNuqta raqamlari (masalan: 145-165, 171, 182-260): ")
            {
                AllowSpaces = true
            };
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
            {
                ed.WriteMessage("\nRaqamlar kiritilmadi. Bekor qilindi.");
                return;
            }

            List<int> numbers = ParseNumbers(pr.StringResult);
            if (numbers.Count < 2)
            {
                ed.WriteMessage("\nKamida 2 ta raqam kerak. Bekor qilindi.");
                return;
            }

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 2) Chizmadagi bloklardan raqam -> koordinata xaritasini quramiz
                Dictionary<int, Point3d> map = BuildPointMap(tr, ms);
                if (map.Count == 0)
                {
                    ed.WriteMessage("\nChizmada raqamli nuqta (atributli blok) topilmadi. "
                                  + "Nuqtalar atributli blok ekanligini tekshiring.");
                    tr.Commit();
                    return;
                }

                // 3) Berilgan tartibda nuqtalarni yig'amiz
                var verts = new List<Point3d>();
                var missing = new List<int>();
                int prev = int.MinValue;
                foreach (int num in numbers)
                {
                    if (num == prev) continue; // ketma-ket dublikatni o'tkazamiz
                    prev = num;

                    Point3d p;
                    if (map.TryGetValue(num, out p))
                        verts.Add(p);
                    else
                        missing.Add(num);
                }

                if (verts.Count < 2)
                {
                    ed.WriteMessage("\nYetarli nuqta topilmadi (" + verts.Count + " ta). "
                                  + "Topilmagan: " + JoinInts(missing));
                    tr.Commit();
                    return;
                }

                // 4) Yopiq poliliniya chizamiz
                var pl = new Polyline();
                pl.SetDatabaseDefaults();
                for (int i = 0; i < verts.Count; i++)
                    pl.AddVertexAt(i, new Point2d(verts[i].X, verts[i].Y), 0.0, 0.0, 0.0);
                pl.Closed = verts.Count >= 3;

                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                tr.Commit();

                // 5) Natijani xabar qilamiz
                var sb = new StringBuilder();
                sb.Append("\n").Append(verts.Count).Append(" ta nuqtadan poligon yasaldi.");
                if (missing.Count > 0)
                    sb.Append(" Topilmagan raqamlar: ").Append(JoinInts(missing));
                ed.WriteMessage(sb.ToString());
            }
        }

        /// <summary>
        /// "145-165, 171, 182-260" kabi ifodani tartiblangan raqamlar ro'yxatiga aylantiradi.
        /// Vergul/nuqta-vergul bilan ajratiladi; "a-b" - diapazon (a..b, ikki tomon inklyuziv).
        /// </summary>
        public static List<int> ParseNumbers(string expr)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(expr)) return result;

            // Uzun tire (en/em dash) va bo'sh joylarni normallashtiramiz
            expr = expr.Replace('\u2013', '-').Replace('\u2014', '-');

            foreach (string partRaw in expr.Split(',', ';'))
            {
                string part = partRaw.Trim();
                if (part.Length == 0) continue;

                int dash = part.IndexOf('-');
                if (dash > 0) // "a-b" diapazon
                {
                    string sa = part.Substring(0, dash).Trim();
                    string sb = part.Substring(dash + 1).Trim();
                    int a, b;
                    if (int.TryParse(sa, out a) && int.TryParse(sb, out b))
                    {
                        if (a <= b)
                            for (int i = a; i <= b; i++) result.Add(i);
                        else
                            for (int i = a; i >= b; i--) result.Add(i);
                    }
                }
                else // yakka raqam
                {
                    int v;
                    if (int.TryParse(part, out v)) result.Add(v);
                }
            }
            return result;
        }

        /// <summary>
        /// Model space dagi atributli bloklardan raqam -> insert nuqtasi xaritasini quradi.
        /// Blokning butun sonli (masalan "145") atributi tartib raqami deb qabul qilinadi
        /// (o'nlik sonli balandlik yoki matnli tavsif e'tiborga olinmaydi).
        /// </summary>
        private static Dictionary<int, Point3d> BuildPointMap(Transaction tr, BlockTableRecord ms)
        {
            var map = new Dictionary<int, Point3d>();

            foreach (ObjectId id in ms)
            {
                var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (br == null || br.AttributeCollection.Count == 0) continue;

                int num;
                if (TryGetPointNumber(tr, br, out num) && !map.ContainsKey(num))
                    map[num] = br.Position;
            }

            return map;
        }

        /// <summary>Blok atributlaridan birinchi butun sonli qiymatni tartib raqami sifatida oladi.</summary>
        private static bool TryGetPointNumber(Transaction tr, BlockReference br, out int number)
        {
            number = 0;
            foreach (ObjectId attId in br.AttributeCollection)
            {
                var att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                if (att == null) continue;

                string s = (att.TextString ?? string.Empty).Trim();
                if (s.Length == 0) continue;

                if (IsPureInteger(s) && int.TryParse(s, out number))
                    return true;
            }
            return false;
        }

        /// <summary>Satr faqat raqamlardan iboratmi (ishorasiz, o'nliksiz).</summary>
        private static bool IsPureInteger(string s)
        {
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return s.Length > 0;
        }

        private static string JoinInts(List<int> list)
        {
            if (list == null || list.Count == 0) return "-";
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(list[i]);
            }
            return sb.ToString();
        }
    }
}
