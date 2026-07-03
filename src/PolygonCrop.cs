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
    /// "Poligondan ajratish" funksiyasi. Foydalanuvchi avval ICHKI, so'ng TASHQI yopiq
    /// poliliniyani TANLAYDI. Tashqi poligon ichidagi obyektlar quyidagicha qayta ishlanadi:
    ///   - ichki poligon ICHIDAGI qismlar QOLADI,
    ///   - ichki poligon TASHQARISIDAGI qismlar O'CHIRILADI,
    ///   - ichki chegarani KESIB o'tgan chiziqlar chegara bo'ylab KESILADI (ichki bo'lagi qoladi).
    /// Tashqi poligondan tashqaridagilar tegilmaydi. Ikki poligonning o'zi o'chirilmaydi.
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

            ObjectId innerId = PromptPolyline(ed, "\nIchki poligonni tanlang (qoldiriladigan soha): ");
            if (innerId.IsNull) { ed.WriteMessage("\nIchki poligon tanlanmadi. Bekor qilindi."); return; }

            ObjectId outerId = PromptPolyline(ed, "\nTashqi poligonni tanlang (tozalash chegarasi): ");
            if (outerId.IsNull) { ed.WriteMessage("\nTashqi poligon tanlanmadi. Bekor qilindi."); return; }

            if (innerId == outerId)
            {
                ed.WriteMessage("\nIchki va tashqi poligon bir xil bo'lmasligi kerak.");
                return;
            }

            int deleted = 0, trimmed = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var innerPl = tr.GetObject(innerId, OpenMode.ForRead) as Polyline;
                var outerPl = tr.GetObject(outerId, OpenMode.ForRead) as Polyline;
                if (innerPl == null || outerPl == null)
                {
                    ed.WriteMessage("\nPoligonlar LWPOLYLINE bo'lishi kerak.");
                    tr.Commit();
                    return;
                }

                List<Point2d> innerPoly = PolyVertices(innerPl);
                Point3dCollection outerPts = PolyPoints(outerPl);
                if (innerPoly.Count < 3 || outerPts.Count < 3)
                {
                    ed.WriteMessage("\nPoligonlar kamida 3 cho'qqili bo'lishi kerak.");
                    tr.Commit();
                    return;
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Tashqi poligon ichida to'liq joylashgan obyektlar (qayta ishlash sohasi)
                PromptSelectionResult inOuter = ed.SelectWindowPolygon(outerPts);
                if (inOuter.Status != PromptStatus.OK || inOuter.Value == null)
                {
                    ed.WriteMessage("\nTashqi poligon ichida obyekt topilmadi.");
                    tr.Commit();
                    return;
                }

                foreach (SelectedObject so in inOuter.Value)
                {
                    if (so == null) continue;
                    if (so.ObjectId == innerId || so.ObjectId == outerId) continue; // poligonlarni saqlaymiz

                    try
                    {
                        ProcessEntity(tr, ms, so.ObjectId, innerPl, innerPoly, ref deleted, ref trimmed);
                    }
                    catch
                    {
                        // Bitta obyekt xato bersa - o'tkazamiz.
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
                try { curve.IntersectWith(innerBoundary, Intersect.OnBothOperands, ips, IntPtr.Zero, IntPtr.Zero); }
                catch { ips = new Point3dCollection(); }

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

                DBObjectCollection pieces = null;
                try { pieces = curve.GetSplitCurves(ips); } catch { pieces = null; }

                if (pieces == null || pieces.Count == 0)
                {
                    if (!IsInside(MidPoint(curve), innerPoly)) { Erase(tr, id); deleted++; }
                    return;
                }

                bool anyKept = false, anyRemoved = false;
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
                        anyRemoved = true;
                        SafeDispose(o);
                    }
                }

                Erase(tr, id);
                if (anyKept && anyRemoved) trimmed++;
                else if (!anyKept) deleted++;
            }
            else
            {
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

        private static List<Point2d> PolyVertices(Polyline pl)
        {
            var list = new List<Point2d>(pl.NumberOfVertices);
            for (int i = 0; i < pl.NumberOfVertices; i++)
                list.Add(pl.GetPoint2dAt(i));
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
            try { if (o != null && !o.IsDisposed && o.ObjectId.IsNull) o.Dispose(); }
            catch { }
        }

        private static ObjectId PromptPolyline(Editor ed, string message)
        {
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage("\nFaqat yopiq poliliniya (LWPOLYLINE) tanlang.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            return per.Status == PromptStatus.OK ? per.ObjectId : ObjectId.Null;
        }
    }
}
