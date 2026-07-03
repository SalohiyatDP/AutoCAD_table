using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

// Yangi buyruq klassini AutoCAD ga ro'yxatdan o'tkazamiz (mavjud kodga tegmasdan).
[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.TomorqaCommands))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Tomorqa" yeri funksiyasi. Foydalanuvchi yopiq maydon ichidan nuqta ko'rsatadi;
    /// tashqi chegara aniqlanadi (BOUNDARY kabi), undan ichkariga sozlamadagi masofa
    /// (standart 2 m) surilgan punktir poligon chiziladi va markaziga "Tomorqa" yoziladi.
    ///
    /// Masofa va yozuv balandligi sozlamalardan (PTSOZLAMA) olinadi.
    /// </summary>
    public class TomorqaCommands
    {
        private const string TomorqaText = "Tomorqa";
        private const string DashedLinetype = "DASHED";

        [CommandMethod("PTTOMORQA")]
        public void CreateTomorqa()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PluginSettings s = PluginSettings.Load();
            double offset = s.TomorqaOffset > 0 ? s.TomorqaOffset : 2.0;
            double th = s.TomorqaTextHeight > 0 ? s.TomorqaTextHeight : 2.5;

            PromptPointResult ppr = ed.GetPoint("\nTomorqa maydoni ichidan nuqta ko'rsating: ");
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nBekor qilindi.");
                return;
            }

            // 1) Chegarani aniqlaymiz (BOUNDARY)
            DBObjectCollection bdy;
            try { bdy = ed.TraceBoundary(ppr.Value, false); }
            catch { bdy = null; }

            if (bdy == null || bdy.Count == 0)
            {
                ed.WriteMessage("\nYopiq maydon topilmadi. Ko'rsatilgan nuqta atrofi to'liq o'ralganligini tekshiring.");
                return;
            }

            // Eng katta yopiq poliliniyani olamiz, qolganini yo'q qilamiz
            Polyline boundary = ExtractLargest(bdy);
            if (boundary == null)
            {
                ed.WriteMessage("\nChegara poliliniyasi olinmadi (Region qaytgan bo'lishi mumkin).");
                return;
            }

            EnsureLinetype(db, DashedLinetype);

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Offset ishonchli bo'lishi uchun chegarani vaqtincha bazaga qo'shamiz
                boundary.Closed = true;
                ms.AppendEntity(boundary);
                tr.AddNewlyCreatedDBObject(boundary, true);

                // 2) Ichki (kichikroq yuzali) offsetni tanlaymiz
                DBObjectCollection inner = GetInnerOffset(boundary);
                if (inner == null || inner.Count == 0)
                {
                    boundary.Erase();
                    tr.Commit();
                    ed.WriteMessage("\nIchki poligon yasab bo'lmadi. Masofa (" + offset
                                  + ") maydonga nisbatan juda katta bo'lishi mumkin.");
                    return;
                }

                // 3) Natijani chizamiz (punktir), eng katta bo'lakni topamiz
                Polyline biggest = null;
                double bestArea = -1.0;
                foreach (DBObject o in inner)
                {
                    Polyline p = o as Polyline;
                    if (p == null) { SafeDispose(o); continue; }

                    p.Closed = true;
                    TrySetDashed(p);
                    ms.AppendEntity(p);
                    tr.AddNewlyCreatedDBObject(p, true);

                    double a = Math.Abs(SafeArea(p));
                    if (a > bestArea) { bestArea = a; biggest = p; }
                }

                // 4) Markaziga "Tomorqa" yozuvi
                if (biggest != null)
                {
                    Point2d c = Centroid(biggest);
                    var t = new DBText
                    {
                        TextString = TomorqaText,
                        Height = th,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        Position = new Point3d(c.X, c.Y, 0.0)
                    };
                    t.AlignmentPoint = new Point3d(c.X, c.Y, 0.0);
                    ms.AppendEntity(t);
                    tr.AddNewlyCreatedDBObject(t, true);
                }

                // Vaqtincha chegarani o'chiramiz (foydalanuvchining asl chizmasi qoladi)
                boundary.Erase();

                tr.Commit();
            }

            ed.WriteMessage("\nTomorqa poligoni chizildi (ichkariga " + offset + " m).");
        }

        /// <summary>Ichki offset: +d va -d dan yuzasi kichikrog'ini tanlaydi.</summary>
        private DBObjectCollection GetInnerOffset(Polyline boundary)
        {
            PluginSettings s = PluginSettings.Load();
            double d = s.TomorqaOffset > 0 ? s.TomorqaOffset : 2.0;

            DBObjectCollection plus = null, minus = null;
            try { plus = boundary.GetOffsetCurves(d); } catch { }
            try { minus = boundary.GetOffsetCurves(-d); } catch { }

            double ap = TotalArea(plus);
            double am = TotalArea(minus);

            bool plusValid = plus != null && plus.Count > 0;
            bool minusValid = minus != null && minus.Count > 0;

            DBObjectCollection inner, outer;
            if (plusValid && (!minusValid || ap <= am)) { inner = plus; outer = minus; }
            else { inner = minus; outer = plus; }

            DisposeNonResident(outer);
            return inner;
        }

        private static double TotalArea(DBObjectCollection col)
        {
            if (col == null) return 0.0;
            double sum = 0.0;
            foreach (DBObject o in col)
            {
                Polyline p = o as Polyline;
                if (p != null) sum += Math.Abs(SafeArea(p));
            }
            return sum;
        }

        private static Polyline ExtractLargest(DBObjectCollection col)
        {
            Polyline best = null;
            double bestA = -1.0;
            foreach (DBObject o in col)
            {
                Polyline p = o as Polyline;
                if (p == null) continue;
                double a = Math.Abs(SafeArea(p));
                if (a > bestA) { bestA = a; best = p; }
            }
            // Ishlatilmaydiganlarni yo'q qilamiz
            foreach (DBObject o in col)
                if (!ReferenceEquals(o, best)) SafeDispose(o);

            return best;
        }

        private static Point2d Centroid(Polyline p)
        {
            int n = p.NumberOfVertices;
            if (n == 0) return Point2d.Origin;

            var pts = new List<Point2d>(n);
            for (int i = 0; i < n; i++) pts.Add(p.GetPoint2dAt(i));

            double a = 0.0, cx = 0.0, cy = 0.0;
            for (int i = 0; i < n; i++)
            {
                Point2d p0 = pts[i];
                Point2d p1 = pts[(i + 1) % n];
                double cross = p0.X * p1.Y - p1.X * p0.Y;
                a += cross;
                cx += (p0.X + p1.X) * cross;
                cy += (p0.Y + p1.Y) * cross;
            }
            a *= 0.5;

            if (Math.Abs(a) < 1e-9)
            {
                double sx = 0.0, sy = 0.0;
                foreach (Point2d q in pts) { sx += q.X; sy += q.Y; }
                return new Point2d(sx / n, sy / n);
            }

            return new Point2d(cx / (6.0 * a), cy / (6.0 * a));
        }

        private static double SafeArea(Polyline p)
        {
            try { return p.Area; } catch { return 0.0; }
        }

        private static void TrySetDashed(Entity ent)
        {
            try { ent.Linetype = DashedLinetype; } catch { /* continuous qoladi */ }
        }

        private static void EnsureLinetype(Database db, string name)
        {
            bool has;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                has = lt.Has(name);
                tr.Commit();
            }
            if (!has)
            {
                try { db.LoadLineTypeFile(name, "acad.lin"); } catch { }
            }
        }

        private static void DisposeNonResident(DBObjectCollection col)
        {
            if (col == null) return;
            foreach (DBObject o in col) SafeDispose(o);
        }

        private static void SafeDispose(DBObject o)
        {
            try
            {
                if (o != null && !o.IsDisposed && o.ObjectId.IsNull)
                    o.Dispose();
            }
            catch { }
        }
    }
}
