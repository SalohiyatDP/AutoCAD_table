using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;

[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.Commands))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Plagin buyruqlari:
    ///   PTABLE    - nuqtalarni sichqoncha bilan ketma-ket ko'rsatib jadval yasash
    ///   PLTABLE   - mavjud poliliniya cho'qqilaridan jadval yasash
    ///   PTCHEGARA - nuqtalardan chegaradoshlar jadvali (faqat raqamlar)
    ///   PLCHEGARA - poliliniyadan chegaradoshlar jadvali
    ///   PTSOZLAMA - sozlamalar oynasi (matn balandligi, xonalar, nuqta belgisi, burchak)
    ///   PTHAQIDA  - plagin va mualliflar haqida
    /// Matn balandligi, o'nlik xonalar va nuqta belgisi turi sozlamalardan olinadi
    /// (PTSOZLAMA orqali o'zgartiriladi va saqlanadi).
    /// </summary>
    public class Commands
    {
        [CommandMethod("PTABLE")]
        public void PointTable()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<Point2d> pts = PointCollector.PickPoints(ed);
            if (pts == null || pts.Count < 2)
            {
                ed.WriteMessage("\nKamida 2 ta nuqta kerak. Buyruq bekor qilindi.");
                return;
            }

            RunBuild(doc, ed, db, pts);
        }

        [CommandMethod("PLTABLE")]
        public void PolylineTable()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<Point2d> pts;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                pts = PointCollector.FromPolyline(ed, tr);
                tr.Commit();
            }

            if (pts == null || pts.Count < 2)
            {
                ed.WriteMessage("\nPoliliniya tanlanmadi yoki cho'qqilari yetarli emas. Bekor qilindi.");
                return;
            }

            RunBuild(doc, ed, db, pts);
        }

        [CommandMethod("PTCHEGARA")]
        public void NeighborsTablePoints()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<Point2d> pts = PointCollector.PickPoints(ed);
            if (pts == null || pts.Count < 2)
            {
                ed.WriteMessage("\nKamida 2 ta nuqta kerak. Buyruq bekor qilindi.");
                return;
            }

            RunNeighbors(doc, ed, db, pts);
        }

        [CommandMethod("PLCHEGARA")]
        public void NeighborsTablePolyline()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<Point2d> pts;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                pts = PointCollector.FromPolyline(ed, tr);
                tr.Commit();
            }

            if (pts == null || pts.Count < 2)
            {
                ed.WriteMessage("\nPoliliniya tanlanmadi yoki cho'qqilari yetarli emas. Bekor qilindi.");
                return;
            }

            RunNeighbors(doc, ed, db, pts);
        }

        [CommandMethod("PTSOZLAMA")]
        public void ShowSettings()
        {
            PluginSettings s = PluginSettings.Load();
            using (var form = new SettingsForm(s))
                AcadApp.ShowModalDialog(form);
        }

        [CommandMethod("PTHAQIDA")]
        public void ShowAbout()
        {
            using (var form = new AboutForm())
                AcadApp.ShowModalDialog(form);
        }

        private static void RunBuild(AcadDoc doc, Editor ed, Database db, List<Point2d> pts)
        {
            // Sozlamalarni yuklaymiz (matn balandligi, xonalar, nuqta belgisi)
            PluginSettings s = PluginSettings.Load();
            TableOptions opt = TableOptions.FromSettings(s);

            // Jadval joyi (tanlangan burchak sozlamalardan)
            var ppo = new PromptPointOptions("\nJadval joyini ko'rsating (tanlangan burchak): ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nJadval joyi ko'rsatilmadi. Bekor qilindi.");
                return;
            }
            Point3d loc = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MarkerDrawer.Draw(tr, db, pts, opt.TextHeight, opt.LabelOffset, opt.Marker, opt.MarkerSize);
                TableBuilder.Build(tr, db, pts, loc, opt);
                tr.Commit();
            }

            ed.WriteMessage("\n" + pts.Count + " ta nuqta uchun jadval yaratildi. "
                          + "(Sozlamalar: PTSOZLAMA)");
        }

        private static void RunNeighbors(AcadDoc doc, Editor ed, Database db, List<Point2d> pts)
        {
            PluginSettings s = PluginSettings.Load();
            TableOptions opt = TableOptions.FromSettingsForNeighbors(s);

            var ppo = new PromptPointOptions("\nChegaradoshlar jadvali joyini ko'rsating (tanlangan burchak): ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nJadval joyi ko'rsatilmadi. Bekor qilindi.");
                return;
            }
            Point3d loc = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                NeighborsTableBuilder.Build(tr, db, pts, loc, opt);
                tr.Commit();
            }

            int segCount = (pts.Count >= 3) ? pts.Count : (pts.Count - 1);
            ed.WriteMessage("\nChegaradoshlar jadvali yaratildi (" + segCount + " ta segment).");
        }
    }
}
