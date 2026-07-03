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
    /// "Poligondan ajratish" funksiyasi. Foydalanuvchi avval ICHKI poligonni, so'ng TASHQI
    /// poligonni CHIZADI (nuqtalarni ko'rsatib, Enter bilan tugatadi). Natijada:
    ///   - ichki poligon ichida to'liq joylashgan chizma/yozuvlar QOLADI,
    ///   - ichki poligondan tashqaridagi (ichki chegarani kesib o'tuvchilar ham) barcha
    ///     obyektlar O'CHIRILADI, ammo faqat TASHQI poligon ichida,
    ///   - tashqi poligondan tashqaridagilar TEGILMAYDI.
    /// Chizilgan poligonlar vaqtinchalik (chizmada qolmaydi).
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

            // 1) Ichki poligonni chizish
            ed.WriteMessage("\n--- Ichki poligonni chizing (qoldiriladigan soha) ---");
            Point3dCollection innerPts = PickPolygon(ed, "Ichki poligon");
            if (innerPts == null || innerPts.Count < 3)
            {
                ed.WriteMessage("\nIchki poligon uchun kamida 3 nuqta kerak. Bekor qilindi.");
                return;
            }

            // 2) Tashqi poligonni chizish
            ed.WriteMessage("\n--- Tashqi poligonni chizing (tozalash chegarasi) ---");
            Point3dCollection outerPts = PickPolygon(ed, "Tashqi poligon");
            if (outerPts == null || outerPts.Count < 3)
            {
                ed.WriteMessage("\nTashqi poligon uchun kamida 3 nuqta kerak. Bekor qilindi.");
                return;
            }

            int deleted = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Tashqi poligon ichida TO'LIQ joylashgan obyektlar (o'chirishga nomzod).
                // Tashqi chegarani kesib chiquvchilar tegilmaydi.
                PromptSelectionResult inOuter = ed.SelectWindowPolygon(outerPts);

                // Ichki poligon ichida TO'LIQ joylashgan obyektlar (saqlanadi).
                // Ichki chegarani kesib o'tuvchilar saqlanmaydi -> o'chadi.
                PromptSelectionResult inInner = ed.SelectWindowPolygon(innerPts);

                if (inOuter.Status != PromptStatus.OK || inOuter.Value == null)
                {
                    ed.WriteMessage("\nTashqi poligon ichida obyekt topilmadi.");
                    tr.Commit();
                    return;
                }

                var keep = new HashSet<ObjectId>();
                if (inInner.Status == PromptStatus.OK && inInner.Value != null)
                    foreach (SelectedObject so in inInner.Value)
                        if (so != null) keep.Add(so.ObjectId);

                foreach (SelectedObject so in inOuter.Value)
                {
                    if (so == null) continue;
                    if (keep.Contains(so.ObjectId)) continue; // ichkaridagini saqlaymiz

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
