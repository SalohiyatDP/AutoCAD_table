using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Nuqtalar bo'yicha jadvalni CHIZIQ va MATN yordamida qo'lda chizadi.
    /// Qator balandligi matn balandligiga, ustun kengligi esa katakdagi eng uzun
    /// matnga moslashadi (auto-fit). Jadval ostiga yer maydoni va chegara uzunligi yoziladi.
    /// (Mualliflar bloki jadvalda emas — menyudagi "Haqida" bo'limida.)
    /// </summary>
    internal static class TableBuilder
    {
        public static void Build(Transaction tr, Database db, IList<Point2d> pts, Point3d loc, TableOptions opt)
        {
            int n = pts.Count;
            bool closed = n >= 3;
            var ci = CultureInfo.InvariantCulture;

            int segments = closed ? n : (n - 1);
            int dataRows = n + segments + (closed ? 1 : 0);
            int totalRows = 2 + dataRows;

            double rh = opt.RowHeight;
            double th = opt.TextHeight;

            // ---- Ustun kengliklarini matnga moslash ----
            double w0 = TextWidth("Nuqtalar №", opt);
            double w1 = TextWidth("Uzunligi(m)", opt);
            double w2 = TextWidth("X", opt);
            double w3 = TextWidth("Y", opt);

            for (int i = 0; i < n; i++)
            {
                w0 = Math.Max(w0, TextWidth((i + 1).ToString(), opt));
                w2 = Math.Max(w2, TextWidth(pts[i].X.ToString(opt.CoordFormat, ci), opt));
                w3 = Math.Max(w3, TextWidth(pts[i].Y.ToString(opt.CoordFormat, ci), opt));

                Point2d next = (i < n - 1) ? pts[i + 1] : pts[0];
                if ((i < n - 1) || closed)
                    w1 = Math.Max(w1, TextWidth(pts[i].GetDistanceTo(next).ToString(opt.LenFormat, ci), opt));
            }

            // "Geo ma'lumotlar" sarlavhasi 1..3 ustunlarga sig'sin
            double geoWidth = TextWidth("Geo ma'lumotlar", opt);
            double sum123 = w1 + w2 + w3;
            if (sum123 < geoWidth)
            {
                double add = (geoWidth - sum123) / 3.0;
                w1 += add; w2 += add; w3 += add;
            }

            // ---- Chegaralar ----
            double[] X = new double[5];
            X[0] = loc.X;
            X[1] = X[0] + w0;
            X[2] = X[1] + w1;
            X[3] = X[2] + w2;
            X[4] = X[3] + w3;

            double[] Y = new double[totalRows + 1];
            for (int k = 0; k <= totalRows; k++)
                Y[k] = loc.Y - k * rh;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // ---- Chiziqlar ----
            AddLine(tr, ms, X[0], Y[0], X[0], Y[totalRows]);
            AddLine(tr, ms, X[1], Y[0], X[1], Y[totalRows]);
            AddLine(tr, ms, X[2], Y[1], X[2], Y[totalRows]); // 0-qatorda yo'q (Geo birlashgan)
            AddLine(tr, ms, X[3], Y[1], X[3], Y[totalRows]);
            AddLine(tr, ms, X[4], Y[0], X[4], Y[totalRows]);

            AddLine(tr, ms, X[0], Y[0], X[4], Y[0]);
            AddLine(tr, ms, X[1], Y[1], X[4], Y[1]);          // 0-ustunda yo'q (№ birlashgan)
            for (int k = 2; k <= totalRows; k++)
                AddLine(tr, ms, X[0], Y[k], X[4], Y[k]);

            // ---- Sarlavha matnlari ----
            AddText(tr, ms, "Nuqtalar №", Mid(X[0], X[1]), Mid(Y[0], Y[2]), th);
            AddText(tr, ms, "Geo ma'lumotlar", Mid(X[1], X[4]), Mid(Y[0], Y[1]), th);
            AddText(tr, ms, "Uzunligi(m)", Mid(X[1], X[2]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "X", Mid(X[2], X[3]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "Y", Mid(X[3], X[4]), Mid(Y[1], Y[2]), th);

            // ---- Ma'lumot qatorlari ----
            int row = 2;
            for (int i = 0; i < n; i++)
            {
                AddText(tr, ms, (i + 1).ToString(), Mid(X[0], X[1]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[i].X.ToString(opt.CoordFormat, ci), Mid(X[2], X[3]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[i].Y.ToString(opt.CoordFormat, ci), Mid(X[3], X[4]), Mid(Y[row], Y[row + 1]), th);
                row++;

                bool hasNext = (i < n - 1) || closed;
                if (hasNext)
                {
                    Point2d next = (i < n - 1) ? pts[i + 1] : pts[0];
                    double d = pts[i].GetDistanceTo(next);
                    AddText(tr, ms, d.ToString(opt.LenFormat, ci), Mid(X[1], X[2]), Mid(Y[row], Y[row + 1]), th);
                    row++;
                }
            }

            if (closed)
            {
                AddText(tr, ms, "1", Mid(X[0], X[1]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[0].X.ToString(opt.CoordFormat, ci), Mid(X[2], X[3]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[0].Y.ToString(opt.CoordFormat, ci), Mid(X[3], X[4]), Mid(Y[row], Y[row + 1]), th);
                row++;
            }

            // ---- Jadval ostidagi xulosa ----
            double area = GeometryHelper.Area(pts);
            double perim = GeometryHelper.Perimeter(pts, closed);
            double ha = area / 10000.0;

            string s1 = "Yer maydoni: " + area.ToString(opt.AreaFormat, ci) +
                        " m.kv (" + ha.ToString(opt.HaFormat, ci) + " ga)";
            string s2 = "Chegara uzunligi: " + perim.ToString(opt.LenFormat, ci) + " m";

            double cx = Mid(X[0], X[4]);
            double bottomY = Y[totalRows];
            AddText(tr, ms, s1, cx, bottomY - rh * 1.2, th);
            AddText(tr, ms, s2, cx, bottomY - rh * 1.2 - th * 1.8, th);
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

        /// <summary>Matnni berilgan nuqtada gorizontal va vertikal markazlab qo'yadi.</summary>
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
