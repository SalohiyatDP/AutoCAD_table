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
    /// GPS nuqtalaridan berilgan tartib raqamlari bo'yicha poligon (yopiq poliliniya) yasaydi.
    ///
    /// Nuqtalar quyidagi ko'rinishda bo'lishi mumkin:
    ///   - POINT (Точka) obyekti + yonida raqam yozilgan matn (DBText/MText), yoki
    ///   - atributli blok (raqam - butun sonli atribut).
    ///
    /// Foydalanish: PTPOLIGON -> raqamlarni kiriting, masalan:
    ///   145-165, 171, 182-260
    /// Shu raqamli nuqtalar ketma-ket tutashtirilib yopiq poliliniya chiziladi.
    /// Keyin uni PLTABLE / PLCHEGARA bilan ishlatish mumkin.
    /// </summary>
    public class PolygonCommands
    {
        // Raqamli matnni nuqtaga bog'lashda: matndan shu masofagacha bo'lgan eng yaqin
        // POINT qidiriladi (0 = cheksiz, ya'ni har doim eng yaqini olinadi).
        private const double MaxLabelToPointDistance = 0.0;

        [CommandMethod("PTPOLIGON")]
        public void PolygonFromPointNumbers()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

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

                Dictionary<int, Point3d> map = BuildPointMap(tr, ms);
                if (map.Count == 0)
                {
                    ed.WriteMessage("\nChizmada raqamli nuqta topilmadi. "
                                  + "Nuqtalar POINT/blok, raqamlari esa matn (yoki atribut) ekanligini tekshiring.");
                    tr.Commit();
                    return;
                }

                var verts = new List<Point3d>();
                var missing = new List<int>();
                int prev = int.MinValue;
                foreach (int num in numbers)
                {
                    if (num == prev) continue;
                    prev = num;

                    Point3d p;
                    if (map.TryGetValue(num, out p)) verts.Add(p);
                    else missing.Add(num);
                }

                if (verts.Count < 2)
                {
                    ed.WriteMessage("\nYetarli nuqta topilmadi (" + verts.Count + " ta). "
                                  + "Topilmagan: " + JoinInts(missing));
                    tr.Commit();
                    return;
                }

                var pl = new Polyline();
                pl.SetDatabaseDefaults();
                for (int i = 0; i < verts.Count; i++)
                    pl.AddVertexAt(i, new Point2d(verts[i].X, verts[i].Y), 0.0, 0.0, 0.0);
                pl.Closed = verts.Count >= 3;

                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                tr.Commit();

                var sb = new StringBuilder();
                sb.Append("\n").Append(verts.Count).Append(" ta nuqtadan poligon yasaldi.");
                if (missing.Count > 0)
                    sb.Append(" Topilmagan raqamlar: ").Append(JoinInts(missing));
                ed.WriteMessage(sb.ToString());
            }
        }

        /// <summary>
        /// "145-165, 171, 182-260" ifodasini tartiblangan raqamlar ro'yxatiga aylantiradi.
        /// </summary>
        public static List<int> ParseNumbers(string expr)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(expr)) return result;

            expr = expr.Replace('\u2013', '-').Replace('\u2014', '-'); // en/em dash -> '-'

            foreach (string partRaw in expr.Split(',', ';'))
            {
                string part = partRaw.Trim();
                if (part.Length == 0) continue;

                int dash = part.IndexOf('-');
                if (dash > 0)
                {
                    string sa = part.Substring(0, dash).Trim();
                    string sb = part.Substring(dash + 1).Trim();
                    int a, b;
                    if (int.TryParse(sa, out a) && int.TryParse(sb, out b))
                    {
                        if (a <= b) for (int i = a; i <= b; i++) result.Add(i);
                        else for (int i = a; i >= b; i--) result.Add(i);
                    }
                }
                else
                {
                    int v;
                    if (int.TryParse(part, out v)) result.Add(v);
                }
            }
            return result;
        }

        /// <summary>
        /// Model space dan raqam -> koordinata xaritasini quradi:
        ///   1) Atributli bloklar (butun sonli atribut = raqam) -> blok insert nuqtasi.
        ///   2) Raqamli matnlar (DBText/MText) -> eng yaqin POINT koordinatasi
        ///      (POINT bo'lmasa, matn joyi ishlatiladi).
        /// </summary>
        private static Dictionary<int, Point3d> BuildPointMap(Transaction tr, BlockTableRecord ms)
        {
            var map = new Dictionary<int, Point3d>();
            var points = new List<Point3d>();                    // POINT koordinatalari
            var labels = new List<KeyValuePair<int, Point3d>>(); // (raqam, matn joyi)

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                var br = ent as BlockReference;
                if (br != null)
                {
                    int bnum;
                    if (br.AttributeCollection.Count > 0 && TryGetPointNumber(tr, br, out bnum)
                        && !map.ContainsKey(bnum))
                        map[bnum] = br.Position;
                    continue;
                }

                var dbp = ent as DBPoint;
                if (dbp != null) { points.Add(dbp.Position); continue; }

                var dtext = ent as DBText;
                if (dtext != null)
                {
                    int tn;
                    if (TryFirstInteger(dtext.TextString, out tn))
                        labels.Add(new KeyValuePair<int, Point3d>(tn, dtext.Position));
                    continue;
                }

                var mtext = ent as MText;
                if (mtext != null)
                {
                    int tn;
                    if (TryFirstInteger(mtext.Contents, out tn))
                        labels.Add(new KeyValuePair<int, Point3d>(tn, mtext.Location));
                    continue;
                }
            }

            // Raqamli matnlarni eng yaqin POINT bilan bog'laymiz
            foreach (var lab in labels)
            {
                if (map.ContainsKey(lab.Key)) continue; // blokdan topilgan bo'lsa - o'tkazamiz

                if (points.Count > 0)
                {
                    double d;
                    Point3d nearest = NearestPoint(lab.Value, points, out d);
                    if (MaxLabelToPointDistance <= 0.0 || d <= MaxLabelToPointDistance)
                        map[lab.Key] = nearest;
                }
                else
                {
                    map[lab.Key] = lab.Value; // POINT yo'q -> matn joyi
                }
            }

            return map;
        }

        private static bool TryGetPointNumber(Transaction tr, BlockReference br, out int number)
        {
            number = 0;
            foreach (ObjectId attId in br.AttributeCollection)
            {
                var att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                if (att == null) continue;

                string s = (att.TextString ?? string.Empty).Trim();
                if (s.Length > 0 && IsPureInteger(s) && int.TryParse(s, out number))
                    return true;
            }
            return false;
        }

        /// <summary>Matndagi birinchi "sof butun son" tokenini qaytaradi (o'nlik/harfli qiymatlar rad etiladi).</summary>
        private static bool TryFirstInteger(string s, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s)) return false;

            // MText format kodlari va satr ajratgichlar bo'yicha bo'lamiz ('.' ajratgich EMAS ->
            // shuning uchun "874.77" bitta token bo'lib qoladi va sof son emas).
            char[] delims = { '\\', '{', '}', ';', ' ', '\t', '\r', '\n', '/', '|', '%', ':' };
            foreach (string tok in s.Split(delims, StringSplitOptions.RemoveEmptyEntries))
            {
                if (IsPureInteger(tok) && int.TryParse(tok, out value))
                    return true;
            }
            return false;
        }

        private static bool IsPureInteger(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }

        private static Point3d NearestPoint(Point3d from, List<Point3d> pts, out double distance)
        {
            Point3d best = pts[0];
            double bestD = Dist2dSq(from, pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                double d = Dist2dSq(from, pts[i]);
                if (d < bestD) { bestD = d; best = pts[i]; }
            }
            distance = Math.Sqrt(bestD);
            return best;
        }

        private static double Dist2dSq(Point3d a, Point3d b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
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
