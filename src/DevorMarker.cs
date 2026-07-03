using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

// Yangi buyruq klassini AutoCAD ga ro'yxatdan o'tkazamiz (mavjud kodga tegmasdan).
[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.DevorCommands))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Devor belgilash" funksiyasi. Foydalanuvchi devor bo'ylab nuqtalarni ketma-ket
    /// ko'rsatadi (Enter bilan tugatadi); shu nuqtalar bo'yicha poliliniya chiziladi va
    /// unga sozlamadagi devor chiziq turi + masshtab (standart 0.3) + qatlam qo'llanadi.
    /// Natijada devor belgisi (chiziq turi orqali) hosil bo'ladi.
    /// </summary>
    public class DevorCommands
    {
        [CommandMethod("PTDEVOR")]
        public void MarkWall()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PluginSettings s = PluginSettings.Load();
            string ltName = (s.DevorLinetype ?? "").Trim();
            double ltScale = s.DevorLtScale > 0 ? s.DevorLtScale : 0.3;
            string layer = (s.DevorLayer ?? "").Trim();

            // Devor bo'ylab nuqtalar (mavjud PointCollector ni qayta ishlatamiz)
            List<Point2d> pts = PointCollector.PickPoints(ed);
            if (pts == null || pts.Count < 2)
            {
                ed.WriteMessage("\nKamida 2 ta nuqta kerak. Buyruq bekor qilindi.");
                return;
            }

            // Chiziq turini oldindan mavjud qilamiz (kerak bo'lsa yuklaymiz)
            bool ltAvailable = ltName.Length > 0 && EnsureLinetype(db, ltName);

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                if (layer.Length > 0)
                    EnsureLayer(tr, db, layer);

                var pl = new Polyline();
                pl.SetDatabaseDefaults();
                for (int i = 0; i < pts.Count; i++)
                    pl.AddVertexAt(i, pts[i], 0.0, 0.0, 0.0);
                pl.Closed = false;

                if (layer.Length > 0) pl.Layer = layer;
                if (ltAvailable) pl.Linetype = ltName;
                pl.LinetypeScale = ltScale;

                ms.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                tr.Commit();
            }

            ed.WriteMessage("\nDevor belgilandi (" + pts.Count + " nuqta, masshtab " + ltScale + ").");
            if (ltName.Length > 0 && !ltAvailable)
                ed.WriteMessage("\nEslatma: '" + ltName + "' chiziq turi topilmadi. Poliliniya joriy chiziq "
                              + "turida chizildi. Sozlamadagi (PTSOZLAMA) nomni tekshiring.");
        }

        /// <summary>Chiziq turini tekshiradi; bo'lmasa acad.lin/acadiso.lin dan yuklashga urinadi.</summary>
        private static bool EnsureLinetype(Database db, string name)
        {
            if (HasLinetype(db, name)) return true;
            try { db.LoadLineTypeFile(name, "acad.lin"); } catch { }
            if (HasLinetype(db, name)) return true;
            try { db.LoadLineTypeFile(name, "acadiso.lin"); } catch { }
            return HasLinetype(db, name);
        }

        private static bool HasLinetype(Database db, string name)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                bool has = lt.Has(name);
                tr.Commit();
                return has;
            }
        }

        /// <summary>Qatlam bo'lmasa yaratadi.</summary>
        private static void EnsureLayer(Transaction tr, Database db, string layerName)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName)) return;

            lt.UpgradeOpen();
            var ltr = new LayerTableRecord { Name = layerName };
            lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
        }
    }
}
