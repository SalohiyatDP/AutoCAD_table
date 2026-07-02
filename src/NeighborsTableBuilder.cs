using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Chegaradoshlar" jadvalini chizadi (rasmga mos ko'rinishda):
    ///
    ///   Ijrochi: &lt;ism&gt;            ___________
    ///   Buyurtmachi:                  ___________
    ///   +-------------------------------------------+
    ///   | Yer uchastkasining chegara burulish       |
    ///   |         nuqtalari tasnifi                 |
    ///   +---------------+---------------------------+
    ///   | Burulish nuq. |                           |
    ///   +------+--------+     Chegaradoshlar        |
    ///   | dan  | gacha  |                           |
    ///   +------+--------+---------------------------+
    ///   |  1   |   2    |                           |
    ///   |  2   |   3    |                           |
    ///   | ...  |  ...   |                           |
    ///
    /// Jadvalning o'ziga faqat raqamlar qo'yiladi; "Chegaradoshlar" ustuni bo'sh
    /// (qo'lda to'ldiriladi). Ijrochi/Buyurtmachi ma'lumoti sozlamalardan olinadi.
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
            double rh = opt.RowHeight;         // oddiy qator (1 satr)
            double rh2 = th * 2.8;             // ikki satrli qator (sarlavha uchun)
            double pad = opt.ColPadding;

            // Segment chekkalari (dan, gacha)
            var fromNo = new string[segCount];
            var toNo = new string[segCount];
            for (int s = 0; s < segCount; s++)
            {
                int a = s + 1;
                int b = (s + 1 < n) ? s + 2 : 1; // yopiq konturda oxirgisi -> 1
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
            // "Burulish nuqtalari" sarlavhasi ikki ustunga sig'sin (eng uzun so'z: "nuqtalari")
            double needCombined = TextWidth("nuqtalari", opt);
            if (w0 + w1 < needCombined)
            {
                double add = (needCombined - (w0 + w1)) / 2.0;
                w0 += add; w1 += add;
            }
            double w2 = Math.Max(TextWidth("Chegaradoshlar", opt), th * 16.0);

            int totalRows = 3 + segCount; // 0:tasnif, 1:Burulish nuq./Chegaradoshlar, 2:dan/gacha, 3..:data
            double totalW = w0 + w1 + w2;
            double totalH = rh2 + rh2 + (segCount + 1) * rh; // 0-qator(rh2)+1-qator(rh2)+ (dan/gacha + data)

            // ---- Boshlanish nuqtasi (tanlangan burchakka moslab) ----
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
            Y[1] = Y[0] - rh2; // tasnif (sarlavha) qatori
            Y[2] = Y[1] - rh2; // "Burulish nuqtalari" / "Chegaradoshlar" qatori
            for (int k = 3; k <= totalRows; k++)
                Y[k] = Y[k - 1] - rh;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // ---- Chiziqlar ----
            // Vertikal
            AddLine(tr, ms, X[0], Y[0], X[0], Y[totalRows]);           // chap
            AddLine(tr, ms, X[1], Y[2], X[1], Y[totalRows]);           // dan|gacha (2-qatordan)
            AddLine(tr, ms, X[2], Y[1], X[2], Y[totalRows]);           // nuqtalar|Chegaradoshlar (1-qatordan)
            AddLine(tr, ms, X[3], Y[0], X[3], Y[totalRows]);           // o'ng
            // Gorizontal
            AddLine(tr, ms, X[0], Y[0], X[3], Y[0]);                   // yuqori
            AddLine(tr, ms, X[0], Y[1], X[3], Y[1]);                   // tasnif ostidan
            AddLine(tr, ms, X[0], Y[2], X[2], Y[2]);                   // Burulish nuq. ostidan (Chegaradoshlar birlashgan)
            for (int k = 3; k <= totalRows; k++)
                AddLine(tr, ms, X[0], Y[k], X[3], Y[k]);

            // ---- Sarlavha matnlari ----
            AddMText(tr, ms, "Yer uchastkasining chegara burulish nuqtalari tasnifi",
                     Mid(X[0], X[3]), Mid(Y[0], Y[1]), (X[3] - X[0]) - 2 * pad, th);
            AddMText(tr, ms, "Burulish nuqtalari",
                     Mid(X[0], X[2]), Mid(Y[1], Y[2]), (X[2] - X[0]) - 2 * pad, th);
            AddText(tr, ms, "Chegaradoshlar", Mid(X[2], X[3]), Mid(Y[1], Y[3]), th); // 1-2 qatorga birlashgan
            AddText(tr, ms, "dan", Mid(X[0], X[1]), Mid(Y[2], Y[3]), th);
            AddText(tr, ms, "gacha", Mid(X[1], X[2]), Mid(Y[2], Y[3]), th);

            // ---- Ma'lumot qatorlari ----
            for (int s = 0; s < segCount; s++)
            {
                int r = 3 + s;
                AddText(tr, ms, fromNo[s], Mid(X[0], X[1]), Mid(Y[r], Y[r + 1]), th);
                AddText(tr, ms, toNo[s], Mid(X[1], X[2]), Mid(Y[r], Y[r + 1]), th);
                // Chegaradoshlar (X[2]..X[3]) - bo'sh
            }

            // ---- Jadval ustidagi Ijrochi / Buyurtmachi satrlari ----
            double u1 = Y[0] + th * 1.2;               // Buyurtmachi tag chizig'i (jadvalga yaqin)
            double u2 = u1 + th * 2.0;                 // Ijrochi tag chizig'i (yuqorida)

            AddTextLeft(tr, ms, "Buyurtmachi: " + Safe(opt.Buyurtmachi), X[0] + pad, u1 + th * 0.3, th);
            AddLine(tr, ms, X[0], u1, X[3], u1);
            AddTextLeft(tr, ms, "Ijrochi: " + Safe(opt.Ijrochi), X[0] + pad, u2 + th * 0.3, th);
            AddLine(tr, ms, X[0], u2, X[3], u2);
        }

        private static string Safe(string s) => string.IsNullOrEmpty(s) ? "" : s;

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

        /// <summary>Bir qatorli, markazlangan matn.</summary>
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

        /// <summary>Chapga tekislangan matn (Ijrochi/Buyurtmachi uchun).</summary>
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

        /// <summary>Ko'p qatorli (o'raladigan), markazlangan matn - uzun sarlavhalar uchun.</summary>
        private static void AddMText(Transaction tr, BlockTableRecord ms, string text, double cx, double cy, double width, double th)
        {
            var mt = new MText
            {
                TextHeight = th,
                Width = width > 0 ? width : 0,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = new Point3d(cx, cy, 0.0),
                Contents = text
            };
            ms.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
        }
    }
}
