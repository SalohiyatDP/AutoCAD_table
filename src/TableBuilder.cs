using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Nuqtalar bo'yicha jadvalni CHIZIQ va MATN yordamida qo'lda chizadi
    /// (AutoCAD Table obyekti o'rniga), shunda qator balandligi matnga aniq mos keladi.
    /// Jadval ostiga yer maydoni (m.kv, ga) va chegara uzunligi yoziladi.
    ///
    /// Tuzilishi (rasmga mos):
    ///   Sarlavha: | Nuqtalar №  |            Geo ma'lumotlar           |
    ///             |             | Uzunligi(m) |     X      |     Y     |
    ///   Har bir nuqta:  [№] [ - ] [X] [Y]
    ///   Har bir segment: [ - ] [masofa] [ - ] [ - ]
    ///   Yopiq kontur uchun oxirida 1-nuqta koordinatalari takrorlanadi.
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
            int totalRows = 2 + dataRows; // 2 ta sarlavha qatori

            double rh = opt.RowHeight;
            double th = opt.TextHeight;

            // Ustunlarning X chegaralari
            double[] X = new double[5];
            X[0] = loc.X;
            X[1] = X[0] + opt.ColTR;
            X[2] = X[1] + opt.ColLen;
            X[3] = X[2] + opt.ColX;
            X[4] = X[3] + opt.ColY;

            // Qatorlarning Y chegaralari (yuqoridan pastga)
            double[] Y = new double[totalRows + 1];
            for (int k = 0; k <= totalRows; k++)
                Y[k] = loc.Y - k * rh;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // ---- Chiziqlar ----
            // Vertikal chiziqlar
            AddLine(tr, ms, X[0], Y[0], X[0], Y[totalRows]);          // chap chegara
            AddLine(tr, ms, X[1], Y[0], X[1], Y[totalRows]);          // № | Geo
            AddLine(tr, ms, X[2], Y[1], X[2], Y[totalRows]);          // Uzunligi | X  (0-qatorda yo'q - Geo birlashgan)
            AddLine(tr, ms, X[3], Y[1], X[3], Y[totalRows]);          // X | Y          (0-qatorda yo'q)
            AddLine(tr, ms, X[4], Y[0], X[4], Y[totalRows]);          // o'ng chegara

            // Gorizontal chiziqlar
            AddLine(tr, ms, X[0], Y[0], X[4], Y[0]);                  // yuqori chegara
            AddLine(tr, ms, X[1], Y[1], X[4], Y[1]);                  // sarlavha 1|2 (0-ustunda yo'q - № birlashgan)
            for (int k = 2; k <= totalRows; k++)
                AddLine(tr, ms, X[0], Y[k], X[4], Y[k]);              // qolgan barcha qatorlar

            // ---- Sarlavha matnlari ----
            AddText(tr, ms, "Nuqtalar №", Mid(X[0], X[1]), Mid(Y[0], Y[2]), th);       // 2 qatorga birlashgan
            AddText(tr, ms, "Geo ma'lumotlar", Mid(X[1], X[4]), Mid(Y[0], Y[1]), th);  // 3 ustunga birlashgan
            AddText(tr, ms, "Uzunligi(m)", Mid(X[1], X[2]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "X", Mid(X[2], X[3]), Mid(Y[1], Y[2]), th);
            AddText(tr, ms, "Y", Mid(X[3], X[4]), Mid(Y[1], Y[2]), th);

            // ---- Ma'lumot qatorlari ----
            int row = 2;
            for (int i = 0; i < n; i++)
            {
                // Nuqta qatori
                AddText(tr, ms, (i + 1).ToString(), Mid(X[0], X[1]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[i].X.ToString(opt.CoordFormat, ci), Mid(X[2], X[3]), Mid(Y[row], Y[row + 1]), th);
                AddText(tr, ms, pts[i].Y.ToString(opt.CoordFormat, ci), Mid(X[3], X[4]), Mid(Y[row], Y[row + 1]), th);
                row++;

                // Masofa qatori (shu nuqtadan keyingisigacha)
                bool hasNext = (i < n - 1) || closed;
                if (hasNext)
                {
                    Point2d next = (i < n - 1) ? pts[i + 1] : pts[0];
                    double d = pts[i].GetDistanceTo(next);
                    AddText(tr, ms, d.ToString(opt.LenFormat, ci), Mid(X[1], X[2]), Mid(Y[row], Y[row + 1]), th);
                    row++;
                }
            }

            // Yopuvchi qator: 1-nuqta koordinatalari qayta ko'rsatiladi
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
