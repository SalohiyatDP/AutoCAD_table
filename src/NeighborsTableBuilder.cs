using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// "Chegaradoshlar jadvali" ni chizadi. Har bir chegara segmenti nuqta raqamlari
    /// bilan ko'rsatiladi (masalan 1-2, 2-3, ...), "Chegaradosh" ustuni esa qo'lda
    /// to'ldirish uchun bo'sh qoladi. Jadvalga faqat raqamlar qo'yiladi (koordinatasiz).
    ///
    /// Tuzilishi:
    ///   |            Chegaradoshlar            |   (sarlavha, birlashgan)
    ///   |  №  | Chegara nuqtalari | Chegaradosh |
    ///   |  1  |       1-2         |             |
    ///   |  2  |       2-3         |             |
    ///   | ... |       ...         |             |
    /// </summary>
    internal static class NeighborsTableBuilder
    {
        public static void Build(Transaction tr, Database db, IList<Point2d> pts, Point3d loc, TableOptions opt)
        {
            int n = pts.Count;
            bool closed = n >= 3;
            int segCount = closed ? n : (n - 1);
            if (segCount < 1) return;

            double rh = opt.RowHeight;
            double th = opt.TextHeight;

            // Segment yorliqlari ("1-2" kabi) oldindan tayyorlanadi
            var segLabels = new string[segCount];
            for (int s = 0; s < segCount; s++)
            {
                int a = s + 1;
                int b = (s + 1 < n) ? s + 2 : 1; // yopiq konturda oxirgisi -> 1 ga qaytadi
                segLabels[s] = a + "-" + b;
            }

            // ---- Ustun kengliklari (matnga moslash) ----
            double w0 = TextWidth("№", opt);
            double w1 = TextWidth("Chegara nuqtalari", opt);
            double w2 = TextWidth("Chegaradosh", opt);

            for (int s = 0; s < segCount; s++)
            {
                w0 = Math.Max(w0, TextWidth((s + 1).ToString(), opt));
                w1 = Math.Max(w1, TextWidth(segLabels[s], opt));
            }
            // Chegaradosh ustuni qo'lda to'ldiriladi -> yozishga joy qoldiramiz
            w2 = Math.Max(w2, th * 12.0);

            int titleRows = 1;   // "Chegaradoshlar"
            int headerRows = 1;  // №, Chegara nuqtalari, Chegaradosh
            int totalRows = titleRows + headerRows + segCount;

            double totalW = w0 + w1 + w2;
            double totalH = totalRows * rh;

            // ---- Boshlanish (yuqori-chap) nuqtasi: tanlangan burchakka moslab ----
            double originX = loc.X;
            double originY = loc.Y;
            switch (opt.Anchor)
            {
                case TableAnchor.TopRight: originX = loc.X - totalW; break;
                case TableAnchor.BottomLeft: originY = loc.Y + totalH; break;
                case TableAnchor.BottomRight: originX = loc.X - totalW; originY = loc.Y + totalH; break;
            }

            double[] X = { originX, originX + w0, originX + w0 + w1, originX + w0 + w1 + w2 };
            double[] Y = new double[totalRows + 1];
            for (int k = 0; k <= totalRows; k++)
                Y[k] = originY - k * rh;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // ---- Chiziqlar ----
            // Vertikal: chap va o'ng - to'liq; ichki ikkitasi sarlavhadan (Y[1]) pastda
            AddLine(tr, ms, X[0], Y[0], X[0], Y[totalRows]);
            AddLine(tr, ms, X[1], Y[1], X[1], Y[totalRows]);
            AddLine(tr, ms, X[2], Y[1], X[2], Y[totalRows]);
            AddLine(tr, ms, X[3], Y[0], X[3], Y[totalRows]);

            // Gorizontal: barchasi to'liq kenglikda
            for (int k = 0; k <= totalRows; k++)
                AddLine(tr, ms, X[0], Y[k], X[3], Y[k]);

            // ---- Matnlar ----
            AddText(tr, ms, "Chegaradoshlar", Mid(X[0], X[3]), Mid(Y[0], Y[1]), th); // sarlavha
            AddText(tr, ms, "№", Mid(X[0], X[1]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "Chegara nuqtalari", Mid(X[1], X[2]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "Chegaradosh", Mid(X[2], X[3]), Mid(Y[1], Y[2]), th);

            int row = 2;
            for (int s = 0; s < segCount; s++)
            {
                AddText(tr, ms, (s + 1).ToString(), Mid(X[0], X[1]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, segLabels[s], Mid(X[1], X[2]), Mid(Y[row], Y[row + 1]), th);
                // Chegaradosh (X[2]..X[3]) - bo'sh, qo'lda to'ldiriladi
                row++;
            }
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

        private static void AddText(Transaction tr, BlockTableRecord ms, string text, double cx, double cy, double textHeight)
        {
            var t = new DBText
            {
                TextString = text,
                Height = textHeight,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                Position = new Point3d(cx, cy, 0.0)
            };
            t.AlignmentPoint = new Point3d(cx, cy, 0.0);
            ms.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }
    }
}
