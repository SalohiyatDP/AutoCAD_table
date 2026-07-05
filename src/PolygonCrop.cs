using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

// Yangi buyruq klassini AutoCAD ga ro'yxatdan o'tkazamiz (mavjud kodga tegmasdan).
[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.CropCommands))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Poligondan ajratish" funksiyasi. Avval ICHKI, so'ng TASHQI yopiq poliliniya TANLANADI.
    /// Tashqi poligon ichidagi obyektlar:
    ///   - ichki poligon ICHIDAGI qismlar QOLADI,
    ///   - ichki poligon TASHQARISIDAGI qismlar O'CHIRILADI,
    ///   - ichki chegarani KESIB o'tgan chiziq/poliliniyalar chegara bo'ylab KESILADI.
    /// Kesishuv 2D da (Z'ga bog'liqsiz) hisoblanadi.
    /// </summary>
    public class CropCommands
    {
        private const double Eps = 1e-6;

        [CommandMethod("PTAJRAT")]
        public void CropBetweenPolygons()
        {
            if (!SalohiyatDP.AutoCADTable.Licensing.LicenseGate.Ensure()) return;
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            ObjectId innerId = PromptPolyline(ed, "\nIchki poligonni tanlang (qoldiriladigan soha): ");
            if (innerId.IsNull) { ed.WriteMessage("\nIchki poligon tanlanmadi. Bekor qilindi."); return; }

            ObjectId outerId = PromptPolyline(ed, "\nTashqi poligonni tanlang (tozalash chegarasi): ");
            if (outerId.IsNull) { ed.WriteMessage("\nTashqi poligon tanlanmadi. Bekor qilindi."); return; }

            if (innerId == outerId) { ed.WriteMessage("\nIchki va tashqi poligon bir xil bo'lmasligi kerak."); return; }

            int deleted = 0, trimmed = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var innerPl = tr.GetObject(innerId, OpenMode.ForRead) as Polyline;
                var outerPl = tr.GetObject(outerId, OpenMode.ForRead) as Polyline;
                if (innerPl == null || outerPl == null)
                {
                    ed.WriteMessage("\nPoligonlar LWPOLYLINE bo'lishi kerak.");
                    tr.Commit(); return;
                }

                List<Point2d> innerPoly = PolyVertices(innerPl);
                Point3dCollection outerPts = PolyPoints(outerPl);
                if (innerPoly.Count < 3 || outerPts.Count < 3)
                {
                    ed.WriteMessage("\nPoligonlar kamida 3 cho'qqili bo'lishi kerak.");
                    tr.Commit(); return;
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                PromptSelectionResult inOuter = ed.SelectWindowPolygon(outerPts);
                if (inOuter.Status != PromptStatus.OK || inOuter.Value == null)
                {
                    ed.WriteMessage("\nTashqi poligon ichida obyekt topilmadi.");
                    tr.Commit(); return;
                }

                foreach (SelectedObject so in inOuter.Value)
                {
                    if (so == null) continue;
                    if (so.ObjectId == innerId || so.ObjectId == outerId) continue;
                    try { ProcessEntity(tr, ms, so.ObjectId, innerPl, innerPoly, ref deleted, ref trimmed); }
                    catch { }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nPoligondan ajratildi: " + deleted + " ta obyekt o'chirildi, "
                          + trimmed + " ta chiziq kesildi (ichki qismi qoldirildi).");
        }

        private static void ProcessEntity(Transaction tr, BlockTableRecord ms, ObjectId id,
                                           Polyline innerBoundary, List<Point2d> poly,
                                           ref int deleted, ref int trimmed)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null || ent.IsErased) return;

            // 1) Line va LWPOLYLINE - 2D qo'lda kesish (ishonchli, Z'ga bog'liqsiz)
            var segList = GetSegments(ent);
            if (segList != null)
            {
                var keptSegs = new List<Seg>();
                bool anyCross = false;

                foreach (Seg seg in segList)
                {
                    List<double> ts = EdgeCrossings(seg.A, seg.B, poly);
                    if (ts.Count > 0) anyCross = true;

                    var bps = new List<double> { 0.0 };
                    bps.AddRange(ts);
                    bps.Add(1.0);

                    for (int k = 0; k < bps.Count - 1; k++)
                    {
                        double t0 = bps[k], t1 = bps[k + 1];
                        if (t1 - t0 < Eps) continue;
                        Point2d mid = Lerp(seg.A, seg.B, (t0 + t1) / 2.0);
                        if (IsInside(mid, poly))
                            keptSegs.Add(new Seg(Lerp(seg.A, seg.B, t0), Lerp(seg.A, seg.B, t1)));
                    }
                }

                if (!anyCross)
                {
                    // Kesishmaydi: to'liq ichkarida (qoldiramiz) yoki tashqarida (o'chiramiz)
                    if (keptSegs.Count == 0) { Erase(tr, id); deleted++; }
                    // ichkarida bo'lsa asl obyektga tegmaymiz
                    return;
                }

                // Kesishadi: asl o'rniga ichki bo'laklarni Line qilib qo'yamiz
                Erase(tr, id);
                foreach (Seg s in keptSegs)
                {
                    if (s.A.GetDistanceTo(s.B) < Eps) continue;
                    var ln = new Line(new Point3d(s.A.X, s.A.Y, 0.0), new Point3d(s.B.X, s.B.Y, 0.0));
                    ln.SetPropertiesFrom(ent);
                    ms.AppendEntity(ln);
                    tr.AddNewlyCreatedDBObject(ln, true);
                }
                if (keptSegs.Count > 0) trimmed++; else deleted++;
                return;
            }

            // 2) Arc/Circle/Ellipse - IntersectWith orqali kesish
            var curve = ent as Curve;
            if (curve is Arc || curve is Circle || curve is Ellipse)
            {
                var ips = new Point3dCollection();
                try { curve.IntersectWith(innerBoundary, Intersect.OnBothOperands, ips, IntPtr.Zero, IntPtr.Zero); } catch { }

                if (ips.Count == 0)
                {
                    if (!IsInside(To2d(MidPoint(curve)), poly)) { Erase(tr, id); deleted++; }
                    return;
                }

                DBObjectCollection pieces = null;
                try { pieces = curve.GetSplitCurves(ips); } catch { }
                if (pieces == null || pieces.Count == 0)
                {
                    if (!IsInside(To2d(MidPoint(curve)), poly)) { Erase(tr, id); deleted++; }
                    return;
                }

                bool anyKept = false;
                foreach (DBObject o in pieces)
                {
                    var piece = o as Curve;
                    if (piece == null) { SafeDispose(o); continue; }
                    if (IsInside(To2d(MidPoint(piece)), poly))
                    {
                        piece.SetPropertiesFrom(ent);
                        ms.AppendEntity(piece);
                        tr.AddNewlyCreatedDBObject(piece, true);
                        anyKept = true;
                    }
                    else SafeDispose(o);
                }
                Erase(tr, id);
                if (anyKept) trimmed++; else deleted++;
                return;
            }

            // 3) Matn/blok/boshqa - joylashuviga qarab
            if (!IsInside(To2d(RefPoint(ent)), poly))
            {
                Erase(tr, id);
                deleted++;
            }
        }

        /// <summary>Line yoki LWPOLYLINE segmentlari (2D). Boshqa turlar uchun null.</summary>
        private static List<Seg> GetSegments(Entity ent)
        {
            var line = ent as Line;
            if (line != null)
                return new List<Seg> { new Seg(To2d(line.StartPoint), To2d(line.EndPoint)) };

            var pl = ent as Polyline;
            if (pl != null)
            {
                var segs = new List<Seg>();
                int n = pl.NumberOfVertices;
                int count = pl.Closed ? n : n - 1;
                for (int i = 0; i < count; i++)
                {
                    Point2d a = pl.GetPoint2dAt(i);
                    Point2d b = pl.GetPoint2dAt((i + 1) % n);
                    segs.Add(new Seg(a, b));
                }
                return segs;
            }
            return null;
        }

        /// <summary>s-e kesmasining poligon qirralari bilan kesishish t (0..1) qiymatlari.</summary>
        private static List<double> EdgeCrossings(Point2d s, Point2d e, List<Point2d> poly)
        {
            var ts = new List<double>();
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double t;
                if (SegSeg(s, e, poly[j], poly[i], out t) && t > Eps && t < 1.0 - Eps)
                    ts.Add(t);
            }
            ts.Sort();

            // Yaqin qiymatlarni birlashtiramiz
            var res = new List<double>();
            foreach (double t in ts)
                if (res.Count == 0 || t - res[res.Count - 1] > Eps) res.Add(t);
            return res;
        }

        private static bool SegSeg(Point2d p1, Point2d p2, Point2d p3, Point2d p4, out double t)
        {
            t = 0.0;
            double den = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(den) < 1e-12) return false;
            double tt = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / den;
            double uu = ((p1.X - p3.X) * (p1.Y - p2.Y) - (p1.Y - p3.Y) * (p1.X - p2.X)) / den;
            if (tt >= -Eps && tt <= 1.0 + Eps && uu >= -Eps && uu <= 1.0 + Eps) { t = tt; return true; }
            return false;
        }

        private static Point2d Lerp(Point2d a, Point2d b, double t)
        {
            return new Point2d(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        private static void Erase(Transaction tr, ObjectId id)
        {
            var e = tr.GetObject(id, OpenMode.ForWrite, false) as Entity;
            if (e != null && !e.IsErased) e.Erase();
        }

        private static Point3d MidPoint(Curve c)
        {
            try { return c.GetPointAtParameter((c.StartParam + c.EndParam) / 2.0); }
            catch { try { return c.StartPoint; } catch { return Point3d.Origin; } }
        }

        private static Point3d RefPoint(Entity ent)
        {
            var t = ent as DBText; if (t != null) return t.Position;
            var m = ent as MText; if (m != null) return m.Location;
            var br = ent as BlockReference; if (br != null) return br.Position;
            try
            {
                Extents3d ex = ent.GeometricExtents;
                return new Point3d((ex.MinPoint.X + ex.MaxPoint.X) / 2.0, (ex.MinPoint.Y + ex.MaxPoint.Y) / 2.0, 0.0);
            }
            catch { return Point3d.Origin; }
        }

        private static Point2d To2d(Point3d p) { return new Point2d(p.X, p.Y); }

        private static bool IsInside(Point2d p, List<Point2d> poly)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = poly[i].X, yi = poly[i].Y, xj = poly[j].X, yj = poly[j].Y;
                bool cross = ((yi > p.Y) != (yj > p.Y)) &&
                             (p.X < (xj - xi) * (p.Y - yi) / (yj - yi) + xi);
                if (cross) inside = !inside;
            }
            return inside;
        }

        private static List<Point2d> PolyVertices(Polyline pl)
        {
            var list = new List<Point2d>(pl.NumberOfVertices);
            for (int i = 0; i < pl.NumberOfVertices; i++) list.Add(pl.GetPoint2dAt(i));
            return list;
        }

        private static Point3dCollection PolyPoints(Polyline pl)
        {
            var pts = new Point3dCollection();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p = pl.GetPoint2dAt(i);
                pts.Add(new Point3d(p.X, p.Y, 0.0));
            }
            return pts;
        }

        private static void SafeDispose(DBObject o)
        {
            try { if (o != null && !o.IsDisposed && o.ObjectId.IsNull) o.Dispose(); } catch { }
        }

        private static ObjectId PromptPolyline(Editor ed, string message)
        {
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage("\nFaqat yopiq poliliniya (LWPOLYLINE) tanlang.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult per = ed.GetEntity(peo);
            return per.Status == PromptStatus.OK ? per.ObjectId : ObjectId.Null;
        }

        /// <summary>Ikki o'lchovli kesma (segment).</summary>
        private struct Seg
        {
            public Point2d A, B;
            public Seg(Point2d a, Point2d b) { A = a; B = b; }
        }
    }
}
