using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Chegaradoshlar" jadvali (rasmga mos):
    ///   Ijrochi: &lt;ism&gt;      ____________
    ///   Buyurtmachi: ______________________
    ///   +-------------------------------------------+
    ///   | Yer uchastkasining chegara burulish       |
    ///   |         nuqtalari tasnifi                 |
    ///   +---------------+---------------------------+
    ///   | Burulish nuq. |                           |
    ///   +------+--------+     Chegaradoshlar        |
    ///   | dan  | gacha  |                           |
    ///   +------+--------+---------------------------+
    ///   |  1   |   2    | ____________              |
    ///   | ...  |  ...   | ____________              |
    ///
    /// Jadvalga faqat raqamlar qo'yiladi. "Chegaradoshlar" kataklariga va
    /// "Buyurtmachi" satriga qo'lda to'ldirishni osonlashtirish uchun tag chiziq (___)
    /// qo'yiladi. Sarlavha qatorlari balandligi matnga (o'ralgan satrlarga) moslashadi.
    /// </summary>
    internal static class NeighborsTableBuilder
    {
        public static void Build(Transaction tr, Database db, IList<Point2d> pts, Point3d loc, TableOptions opt)
        {
            int n = pts.Count;
            bool closed = n >= 3;
            int segCount = closed ? n : (n - 1);
            if (segCount < 1) return;

            double th = opt.TextHeight;
            double rh = opt.RowHeight;
            double pad = opt.ColPadding;
            double vpad = th * 0.5;

            // Segment chekkalari (dan / gacha)
            var fromNo = new string[segCount];
            var toNo = new string[segCount];
            for (int s = 0; s < segCount; s++)
            {
                int a = s + 1;
                int b = (s + 1 < n) ? s + 2 : 1;
                fromNo[s] = a.ToString();
                toNo[s] = b.ToString();
            }

            // ---- Ustun kengliklari ----
            double w0 = TextWidth("dan", opt);
            double w1 = TextWidth("gacha", opt);
            for (int s = 0; s < segCount; s++)
            {
                w0 = Math.Max(w0, TextWidth(fromNo[s], opt));
                w1 = Math.Max(w1, TextWidth(toNo[s], opt));
            }
            double needCombined = TextWidth("nuqtalari", opt);
            if (w0 + w1 < needCombined)
            {
                double add = (needCombined - (w0 + w1)) / 2.0;
                w0 += add; w1 += add;
            }
            // "Chegaradoshlar" ustuni kengligi - sozlamadan
            double w2 = Math.Max(TextWidth("Chegaradoshlar", opt), opt.NeighborsColWidth);

            double totalW = w0 + w1 + w2;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // ---- Sarlavha MText'lari (balandlikni o'lchash uchun oldin qo'shamiz) ----
            MText titleMt = MakeMText(tr, ms, "Yer uchastkasining chegara burulish nuqtalari tasnifi",
                                      totalW - 2 * pad, th);
            MText burMt = MakeMText(tr, ms, "Burulish nuqtalari", (w0 + w1) - 2 * pad, th);

            double rowTitleH = Math.Max(SafeHeight(titleMt) + 2 * vpad, rh);
            double rowGroupH = Math.Max(SafeHeight(burMt) + 2 * vpad, rh);

            int totalRows = 3 + segCount;
            double totalH = rowTitleH + rowGroupH + (segCount + 1) * rh;

            // ---- Boshlanish nuqtasi (chegaradoshlar burchagi sozlamasi) ----
            double originX = loc.X;
            double originY = loc.Y;
            switch (opt.NeighborsAnchor)
            {
                case TableAnchor.TopRight: originX = loc.X - totalW; break;
                case TableAnchor.BottomLeft: originY = loc.Y + totalH; break;
                case TableAnchor.BottomRight: originX = loc.X - totalW; originY = loc.Y + totalH; break;
            }

            double[] X = { originX, originX + w0, originX + w0 + w1, originX + w0 + w1 + w2 };

            double[] Y = new double[totalRows + 1];
            Y[0] = originY;
            Y[1] = Y[0] - rowTitleH;
            Y[2] = Y[1] - rowGroupH;
            for (int k = 3; k <= totalRows; k++)
                Y[k] = Y[k - 1] - rh;

            // ---- Chiziqlar ----
            AddLine(tr, ms, X[0], Y[0], X[0], Y[totalRows]);
            AddLine(tr, ms, X[1], Y[2], X[1], Y[totalRows]);
            AddLine(tr, ms, X[2], Y[1], X[2], Y[totalRows]);
            AddLine(tr, ms, X[3], Y[0], X[3], Y[totalRows]);

            AddLine(tr, ms, X[0], Y[0], X[3], Y[0]);
            AddLine(tr, ms, X[0], Y[1], X[3], Y[1]);
            AddLine(tr, ms, X[0], Y[2], X[2], Y[2]);
            for (int k = 3; k <= totalRows; k++)
                AddLine(tr, ms, X[0], Y[k], X[3], Y[k]);

            // ---- Sarlavha matnlarini joyiga qo'yish ----
            titleMt.Location = new Point3d(Mid(X[0], X[3]), Mid(Y[0], Y[1]), 0.0);
            burMt.Location = new Point3d(Mid(X[0], X[2]), Mid(Y[1], Y[2]), 0.0);

            AddText(tr, ms, "Chegaradoshlar", Mid(X[2], X[3]), Mid(Y[1], Y[3]), th);
            AddText(tr, ms, "dan", Mid(X[0], X[1]), Mid(Y[2], Y[3]), th);
            AddText(tr, ms, "gacha", Mid(X[1], X[2]), Mid(Y[2], Y[3]), th);

            // ---- Ma'lumot qatorlari (raqamlar + Chegaradoshlar uchun tag chiziq) ----
            int fillCount = Math.Max(3, (int)((w2 - 2 * pad) / opt.CharWidth));
            string fillLine = new string('_', fillCount);
            for (int s = 0; s < segCount; s++)
            {
                int r = 3 + s;
                AddText(tr, ms, fromNo[s], Mid(X[0], X[1]), Mid(Y[r], Y[r + 1]), th);
                AddText(tr, ms, toNo[s], Mid(X[1], X[2]), Mid(Y[r], Y[r + 1]), th);
                // Chegaradoshlar katagi: chapdan tag chiziq (qo'lda yozish uchun)
                AddTextLeft(tr, ms, fillLine, X[2] + pad, Mid(Y[r], Y[r + 1]) - th * 0.2, th);
            }

            // ---- Jadval ustidagi Ijrochi / Buyurtmachi ----
            int buyurtFill = Math.Max(5, (int)((totalW - 2 * pad) / opt.CharWidth) - "Buyurtmachi: ".Length);
            string buyurtLine = new string('_', buyurtFill);

            double yBuyurt = Y[0] + th * 1.0;                 // jadvalga yaqin
            double yIjrochi = yBuyurt + th * 2.2;             // yuqorida
            AddTextLeft(tr, ms, "Buyurtmachi: " + buyurtLine, X[0] + pad, yBuyurt, th);
            AddTextLeft(tr, ms, "Ijrochi: " + Safe(opt.Ijrochi), X[0] + pad, yIjrochi, th);
            AddLine(tr, ms, X[0], yIjrochi - th * 0.3, X[3], yIjrochi - th * 0.3); // Ijrochi tag chizig'i
        }

        private static string Safe(string s) => string.IsNullOrEmpty(s) ? "" : s;

        private static double SafeHeight(MText mt)
        {
            try { double h = mt.ActualHeight; return h > 0 ? h : mt.TextHeight; }
            catch { return mt.TextHeight; }
        }

        private static double TextWidth(string s, TableOptions opt)
        {
            int len = string.IsNullOrEmpty(s) ? 0 : s.Length;
            double w = len * opt.CharWidth + 2.0 * opt.ColPadding;
            return Math.Max(w, opt.MinColWidth);
        }

        private static double Mid(double a, double b) => (a + b) / 2.0;

        private static void AddLine(Transaction tr, BlockTableRecord ms, double x1, double y1, double x2, double y2)
        {
            var ln = new Line(new Point3d(x1, y1, 0.0), new Point3d(x2, y2, 0.0));
            ms.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
        }

        private static void AddText(Transaction tr, BlockTableRecord ms, string text, double cx, double cy, double th)
        {
            var t = new DBText
            {
                TextString = text,
                Height = th,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                Position = new Point3d(cx, cy, 0.0)
            };
            t.AlignmentPoint = new Point3d(cx, cy, 0.0);
            ms.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }

        private static void AddTextLeft(Transaction tr, BlockTableRecord ms, string text, double x, double yBase, double th)
        {
            var t = new DBText
            {
                TextString = text,
                Height = th,
                Position = new Point3d(x, yBase, 0.0)
            };
            ms.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }

        private static MText MakeMText(Transaction tr, BlockTableRecord ms, string text, double width, double th)
        {
            var mt = new MText
            {
                TextHeight = th,
                Width = width > 0 ? width : 0,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = new Point3d(0, 0, 0),
                Contents = text
            };
            ms.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
            return mt;
        }
    }
}
