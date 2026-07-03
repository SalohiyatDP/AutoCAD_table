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
    /// "Poligondan ajratish" funksiyasi. Avval ICHKI yopiq poliliniya, so'ng TASHQI yopiq
    /// poliliniya tanlanadi. Natijada:
    ///   - ichki poligon ichidagi chizma/yozuvlar QOLADI,
    ///   - ichki va tashqi poligonlar orasidagi (halqa) barcha chizma/yozuvlar O'CHIRILADI,
    ///   - tashqi poligondan tashqaridagilar TEGILMAYDI.
    /// Ikki poligonning o'zi (chegaralari) o'chirilmaydi.
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

            // 1) Ichki poligon
            ObjectId innerId = PromptPolyline(ed, "\nIchki poligonni tanlang: ");
            if (innerId.IsNull) { ed.WriteMessage("\nIchki poligon tanlanmadi. Bekor qilindi."); return; }

            // 2) Tashqi poligon
            ObjectId outerId = PromptPolyline(ed, "\nTashqi poligonni tanlang: ");
            if (outerId.IsNull) { ed.WriteMessage("\nTashqi poligon tanlanmadi. Bekor qilindi."); return; }

            if (innerId == outerId)
            {
                ed.WriteMessage("\nIchki va tashqi poligon bir xil bo'lmasligi kerak.");
                return;
            }

            int deleted = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Point3dCollection innerPts = GetPolyPoints(tr, innerId);
                Point3dCollection outerPts = GetPolyPoints(tr, outerId);

                if (innerPts == null || innerPts.Count < 3 || outerPts == null || outerPts.Count < 3)
                {
                    ed.WriteMessage("\nPoligonlar yopiq poliliniya (kamida 3 cho'qqi) bo'lishi kerak.");
                    tr.Commit();
                    return;
                }

                // Tashqi ichida to'liq joylashgan obyektlar (o'chirishga nomzod)
                PromptSelectionResult inOuter = ed.SelectWindowPolygon(outerPts);
                // Ichki bilan kesishgan yoki ichidagi obyektlar (saqlanadi)
                PromptSelectionResult inInner = ed.SelectCrossingPolygon(innerPts);

                if (inOuter.Status != PromptStatus.OK || inOuter.Value == null)
                {
                    ed.WriteMessage("\nTashqi poligon ichida obyekt topilmadi.");
                    tr.Commit();
                    return;
                }

                // Saqlanadigan ObjectId'lar to'plami
                var keep = new HashSet<ObjectId>();
                if (inInner.Status == PromptStatus.OK && inInner.Value != null)
                    foreach (SelectedObject so in inInner.Value)
                        if (so != null) keep.Add(so.ObjectId);

                keep.Add(innerId); // poligon chegaralarini saqlaymiz
                keep.Add(outerId);

                foreach (SelectedObject so in inOuter.Value)
                {
                    if (so == null) continue;
                    if (keep.Contains(so.ObjectId)) continue;

                    try
                    {
                        var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite, false) as Entity;
                        if (ent != null && !ent.IsErased)
                        {
                            ent.Erase();
                            deleted++;
                        }
                    }
                    catch
                    {
                        // Bloklangan qatlam yoki o'chirib bo'lmaydigan obyekt - o'tkazamiz.
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nPoligondan ajratildi: " + deleted + " ta obyekt o'chirildi "
                          + "(ichki poligon ichidagilar saqlab qolindi).");
        }

        /// <summary>Yopiq poliliniyani tanlashni so'raydi.</summary>
        private static ObjectId PromptPolyline(Editor ed, string message)
        {
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage("\nFaqat poliliniya (LWPOLYLINE) tanlang.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            return per.Status == PromptStatus.OK ? per.ObjectId : ObjectId.Null;
        }

        /// <summary>Poliliniya cho'qqilaridan Point3dCollection quradi.</summary>
        private static Point3dCollection GetPolyPoints(Transaction tr, ObjectId id)
        {
            var pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
            if (pl == null) return null;

            var pts = new Point3dCollection();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p = pl.GetPoint2dAt(i);
                pts.Add(new Point3d(p.X, p.Y, 0.0));
            }
            return pts;
        }
    }
}
