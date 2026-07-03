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
    /// "Poligondan ajratish" funksiyasi. Foydalanuvchi avval ICHKI, so'ng TASHQI poligonni
    /// CHIZADI (nuqtalarni ko'rsatib, Enter). Tashqi poligon ichidagi obyektlar quyidagicha
    /// qayta ishlanadi:
    ///   - ichki poligon ICHIDAGI qismlar QOLADI,
    ///   - ichki poligon TASHQARISIDAGI qismlar O'CHIRILADI (chegarani kesib o'tgan chiziqlar
    ///     ichki chegara bo'yicha KESILADI - ichki bo'lagi qoladi).
    /// Tashqi poligondan tashqaridagilar tegilmaydi. Chizilgan poligonlar chizmada qolmaydi.
    /// </summary>
    public class CropCommands
    {
        [CommandMethod("PTAJRAT")]
        public void CropBetweenPolygons()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            ed.WriteMessage("\n--- Ichki poligonni chizing (qoldiriladigan soha) ---");
            Point3dCollection innerPts = PickPolygon(ed, "Ichki poligon");
            if (innerPts == null || innerPts.Count < 3)
            {
                ed.WriteMessage("\nIchki poligon uchun kamida 3 nuqta kerak. Bekor qilindi.");
                return;
            }

            ed.WriteMessage("\n--- Tashqi poligonni chizing (tozalash chegarasi) ---");
            Point3dCollection outerPts = PickPolygon(ed, "Tashqi poligon");
            if (outerPts == null || outerPts.Count < 3)
            {
                ed.WriteMessage("\nTashqi poligon uchun kamida 3 nuqta kerak. Bekor qilindi.");
                return;
            }

            var innerPoly = ToList(innerPts);
            int deleted = 0, trimmed = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Tashqi poligon ichida to'liq joylashgan obyektlar (qayta ishlash sohasi).
                PromptSelectionResult inOuter = ed.SelectWindowPolygon(outerPts);
                if (inOuter.Status != PromptStatus.OK || inOuter.Value == null)
                {
                    ed.WriteMessage("\nTashqi poligon ichida obyekt topilmadi.");
                    tr.Commit();
                    return;
                }

                // Ichki poligonni kesish uchun vaqtinchalik (bazaga qo'shilmaydigan) poliliniya
                using (var innerBoundary = MakePolyline(innerPts))
                {
                    foreach (SelectedObject so in inOuter.Value)
                    {
                        if (so == null) continue;
                        try
                        {
                            ProcessEntity(tr, ms, so.ObjectId, innerBoundary, innerPoly, ref deleted, ref trimmed);
                        }
                        catch
                        {
                            // Bitta obyekt xato bersa - o'tkazamiz.
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nPoligondan ajratildi: " + deleted + " ta obyekt o'chirildi, "
                          + trimmed + " ta chiziq kesildi (ichki qismi qoldirildi).");
        }

        private static void ProcessEntity(Transaction tr, BlockTableRecord ms, ObjectId id,
                                           Polyline innerBoundary, List<Point2d> innerPoly,
                                           ref int deleted, ref int trimmed)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null || ent.IsErased) return;

            var curve = ent as Curve;
            bool splittable = curve != null &&
                              (curve is Line || curve is Arc || curve is Polyline ||
                               curve is Polyline2d || curve is Circle || curve is Ellipse);

            if (splittable)
            {
                var ips = new Point3dCollection();
                curve.IntersectWith(innerBoundary, Intersect.OnBothOperands, ips, IntPtr.Zero, IntPtr.Zero);

                if (ips.Count == 0)
                {
                    // Kesishmaydi: to'liq ichkarida yoki to'liq tashqarida
                    if (!IsInside(MidPoint(curve), innerPoly))
                    {
                        Erase(tr, id);
                        deleted++;
                    }
                    return;
                }

                // Kesishadi: bo'laklarga bo'lamiz, ichkaridagilarini qoldiramiz
                DBObjectCollection pieces = null;
                try { pieces = curve.GetSplitCurves(ips); } catch { pieces = null; }

                if (pieces == null || pieces.Count == 0)
                {
                    // Bo'lib bo'lmadi - butun holida ichkarida bo'lsa qoldiramiz, aks holda o'chiramiz
                    if (!IsInside(MidPoint(curve), innerPoly)) { Erase(tr, id); deleted++; }
                    return;
                }

                bool anyKept = false;
                foreach (DBObject o in pieces)
                {
                    var piece = o as Curve;
                    if (piece == null) { SafeDispose(o); continue; }

                    if (IsInside(MidPoint(piece), innerPoly))
                    {
                        piece.SetPropertiesFrom(ent);
                        ms.AppendEntity(piece);
                        tr.AddNewlyCreatedDBObject(piece, true);
                        anyKept = true;
                    }
                    else
                    {
                        SafeDispose(o);
                    }
                }

                Erase(tr, id); // asl obyekt o'rniga bo'laklar qoldi
                if (anyKept) trimmed++; else deleted++;
            }
            else
            {
                // Matn/blok/boshqa: joylashuviga qarab qoldiramiz yoki o'chiramiz
                Point3d refPt = RefPoint(ent);
                if (!IsInside(refPt, innerPoly))
                {
                    Erase(tr, id);
                    deleted++;
                }
            }
        }

        private static void Erase(Transaction tr, ObjectId id)
        {
            var e = tr.GetObject(id, OpenMode.ForWrite, false) as Entity;
            if (e != null && !e.IsErased) e.Erase();
        }

        private static Point3d MidPoint(Curve c)
        {
            try { return c.GetPointAtParameter((c.StartParam + c.EndParam) / 2.0); }
            catch
            {
                try { return c.StartPoint; } catch { return Point3d.Origin; }
            }
        }

        private static Point3d RefPoint(Entity ent)
        {
            var t = ent as DBText;
            if (t != null) return t.Position;
            var m = ent as MText;
            if (m != null) return m.Location;
            var br = ent as BlockReference;
            if (br != null) return br.Position;
            try
            {
                Extents3d ex = ent.GeometricExtents;
                return new Point3d((ex.MinPoint.X + ex.MaxPoint.X) / 2.0,
                                   (ex.MinPoint.Y + ex.MaxPoint.Y) / 2.0, 0.0);
            }
            catch { return Point3d.Origin; }
        }

        private static bool IsInside(Point3d p, List<Point2d> poly)
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

        private static List<Point2d> ToList(Point3dCollection pts)
        {
            var list = new List<Point2d>(pts.Count);
            foreach (Point3d p in pts) list.Add(new Point2d(p.X, p.Y));
            return list;
        }

        private static Polyline MakePolyline(Point3dCollection pts)
        {
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++)
                pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0.0, 0.0, 0.0);
            pl.Closed = true;
            return pl;
        }

        private static void SafeDispose(DBObject o)
        {
            try { if (o != null && !o.IsDisposed && o.ObjectId.IsNull) o.Dispose(); }
            catch { }
        }

        /// <summary>Foydalanuvchi nuqtalarni ketma-ket ko'rsatib poligon chizadi (Enter - tugatish).</summary>
        private static Point3dCollection PickPolygon(Editor ed, string title)
        {
            var list = new List<Point3d>();

            while (true)
            {
                string msg = "\n" + title + " — "
                           + (list.Count == 0 ? "birinchi nuqta" : (list.Count + 1) + "-nuqta")
                           + " (tugatish uchun Enter): ";
                var ppo = new PromptPointOptions(msg) { AllowNone = true };

                if (list.Count > 0)
                {
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = list[list.Count - 1];
                    ppo.UseDashedLine = true;
                }

                PromptPointResult r = ed.GetPoint(ppo);
                if (r.Status == PromptStatus.OK)
                    list.Add(r.Value);
                else
                    break;
            }

            var pts = new Point3dCollection();
            foreach (Point3d p in list)
                pts.Add(new Point3d(p.X, p.Y, 0.0));
            return pts;
        }
    }
}
