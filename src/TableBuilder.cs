using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Nuqtalar bo'yicha AutoCAD Table obyektini yasaydi va uning ostiga
    /// yer maydoni (m.kv, ga) hamda chegara uzunligi matnini qo'yadi.
    ///
    /// Jadval tuzilishi (rasmga mos):
    ///   Sarlavha 1: | Nuqtalar T/R |            Geomalumotlar            |
    ///   Sarlavha 2: |              | Uzunligi(m) |     X     |     Y      |
    ///   Har bir nuqta ikki qatordan iborat:
    ///     - nuqta qatori:  [T/R] [ - ] [X] [Y]
    ///     - masofa qatori: [ - ] [shu nuqtadan keyingisigacha masofa] [ - ] [ - ]
    ///   Yopiq kontur uchun oxirida 1-nuqta koordinatalari yana takrorlanadi.
    /// </summary>
    internal static class TableBuilder
    {
        public static Table Build(Transaction tr, Database db, IList<Point2d> pts, Point3d loc, TableOptions opt)
        {
            int n = pts.Count;
            bool closed = n >= 3;
            var ci = CultureInfo.InvariantCulture;

            // Qatorlar soni:
            //   2 ta sarlavha + har nuqta uchun 1 qator + har segment uchun 1 masofa qatori
            //   + yopiq bo'lsa 1 ta yopuvchi (1-nuqta) qatori.
            int segments = closed ? n : (n - 1);
            int dataRows = n + segments + (closed ? 1 : 0);
            int totalRows = 2 + dataRows;

            var tb = new Table();
            tb.TableStyle = db.Tablestyle;
            tb.Position = loc;
            tb.SetSize(totalRows, 4);

            tb.Columns[0].Width = opt.ColTR;
            tb.Columns[1].Width = opt.ColLen;
            tb.Columns[2].Width = opt.ColX;
            tb.Columns[3].Width = opt.ColY;
            for (int r = 0; r < totalRows; r++)
                tb.Rows[r].Height = opt.RowHeight;

            // Sarlavha kataklarini birlashtirish
            tb.MergeCells(CellRange.Create(tb, 0, 0, 1, 0)); // "Nuqtalar T/R" vertikal
            tb.MergeCells(CellRange.Create(tb, 0, 1, 0, 3)); // "Geomalumotlar" gorizontal

            SetCell(tb, 0, 0, "Nuqtalar T/R", opt.TextHeight);
            SetCell(tb, 0, 1, "Geomalumotlar", opt.TextHeight);
            SetCell(tb, 1, 1, "Uzunligi(m)", opt.TextHeight);
            SetCell(tb, 1, 2, "X", opt.TextHeight);
            SetCell(tb, 1, 3, "Y", opt.TextHeight);

            int row = 2;
            for (int i = 0; i < n; i++)
            {
                // Nuqta qatori
                SetCell(tb, row, 0, (i + 1).ToString(), opt.TextHeight);
                SetCell(tb, row, 2, pts[i].X.ToString(opt.CoordFormat, ci), opt.TextHeight);
                SetCell(tb, row, 3, pts[i].Y.ToString(opt.CoordFormat, ci), opt.TextHeight);
                row++;

                // Masofa qatori (shu nuqtadan keyingisigacha)
                bool hasNext = (i < n - 1) || closed;
                if (hasNext)
                {
                    Point2d next = (i < n - 1) ? pts[i + 1] : pts[0];
                    double d = pts[i].GetDistanceTo(next);
                    SetCell(tb, row, 1, d.ToString(opt.LenFormat, ci), opt.TextHeight);
                    row++;
                }
            }

            // Yopuvchi qator: 1-nuqta koordinatalari qayta ko'rsatiladi
            if (closed)
            {
                SetCell(tb, row, 0, "1", opt.TextHeight);
                SetCell(tb, row, 2, pts[0].X.ToString(opt.CoordFormat, ci), opt.TextHeight);
                SetCell(tb, row, 3, pts[0].Y.ToString(opt.CoordFormat, ci), opt.TextHeight);
                row++;
            }

            tb.GenerateLayout();

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            ms.AppendEntity(tb);
            tr.AddNewlyCreatedDBObject(tb, true);

            // Jadval ostidagi xulosa matni
            double area = GeometryHelper.Area(pts);
            double perim = GeometryHelper.Perimeter(pts, closed);
            double ha = area / 10000.0;

            string s1 = "Yer maydoni: " + area.ToString(opt.AreaFormat, ci) +
                        " m.kv (" + ha.ToString(opt.HaFormat, ci) + " ga)";
            string s2 = "Chegara uzunligi: " + perim.ToString(opt.LenFormat, ci) + " m";

            double tableWidth = opt.ColTR + opt.ColLen + opt.ColX + opt.ColY;
            double bottomY = loc.Y - tb.Height;
            double cx = loc.X + tableWidth / 2.0;
            double gap = opt.RowHeight;

            AddCenteredText(tr, ms, s1, new Point3d(cx, bottomY - gap, 0.0), opt.TextHeight);
            AddCenteredText(tr, ms, s2, new Point3d(cx, bottomY - gap - opt.TextHeight * 1.8, 0.0), opt.TextHeight);

            return tb;
        }

        private static void SetCell(Table tb, int r, int c, string text, double textHeight)
        {
            Cell cell = tb.Cells[r, c];
            cell.TextString = text;
            cell.Alignment = CellAlignment.MiddleCenter;
            cell.TextHeight = textHeight;
        }

        private static void AddCenteredText(Transaction tr, BlockTableRecord ms, string text, Point3d pos, double textHeight)
        {
            var t = new DBText
            {
                TextString = text,
                Height = textHeight,
                Position = pos,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextBase
            };
            t.AlignmentPoint = pos;
            ms.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }
    }
}
