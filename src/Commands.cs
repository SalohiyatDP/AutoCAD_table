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
    ///   PTABLE  - nuqtalarni sichqoncha bilan ketma-ket ko'rsatib jadval yasash
    ///   PLTABLE - mavjud poliliniya cho'qqilaridan jadval yasash
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

        private static void RunBuild(AcadDoc doc, Editor ed, Database db, List<Point2d> pts)
        {
            // Matn balandligini so'rash (jadval o'lchamlari shunga moslashadi)
            var pdo = new PromptDistanceOptions("\nMatn balandligi <2.5>: ")
            {
                AllowNone = true,
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = 2.5,
                UseDefaultValue = true
            };
            PromptDoubleResult pdr = ed.GetDistance(pdo);
            double th = (pdr.Status == PromptStatus.OK) ? pdr.Value : 2.5;
            if (th <= 0) th = 2.5;

            // Nuqta belgisi turini so'rash: Hech / Doira / X
            MarkerType marker = MarkerType.None;
            var pko = new PromptKeywordOptions("\nNuqta belgisi turi ")
            {
                AllowNone = true
            };
            pko.Keywords.Add("Hech");
            pko.Keywords.Add("Doira");
            pko.Keywords.Add("Xbelgi");
            pko.Keywords.Default = "Hech";
            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status == PromptStatus.OK)
            {
                switch (pkr.StringResult)
                {
                    case "Doira": marker = MarkerType.Circle; break;
                    case "Xbelgi": marker = MarkerType.Cross; break;
                    default: marker = MarkerType.None; break;
                }
            }

            // Jadval joyi (yuqori-chap burchak)
            var ppo = new PromptPointOptions("\nJadval joyini ko'rsating (yuqori-chap burchak): ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nJadval joyi ko'rsatilmadi. Bekor qilindi.");
                return;
            }
            Point3d loc = ppr.Value;

            TableOptions opt = TableOptions.FromTextHeight(th);

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MarkerDrawer.Draw(tr, db, pts, opt.TextHeight, opt.LabelOffset, marker, opt.MarkerSize);
                TableBuilder.Build(tr, db, pts, loc, opt);
                tr.Commit();
            }

            ed.WriteMessage("\n" + pts.Count + " ta nuqta uchun jadval yaratildi.");
        }
    }
}
