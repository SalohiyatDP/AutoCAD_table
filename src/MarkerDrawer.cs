using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>Nuqta ustiga qo'yiladigan belgi turi.</summary>
    internal enum MarkerType
    {
        None,   // hech narsa
        Circle, // doira
        Cross   // X belgisi
    }

    /// <summary>
    /// Har bir nuqtaga tanlangan belgini (yo'q / doira / X) chizadi va tartib raqamini
    /// POLIGONDAN TASHQARIGA joylashtiradi (raqam kontur ichida qolmaydi).
    /// </summary>
    internal static class MarkerDrawer
    {
        public static void Draw(Transaction tr, Database db, IList<Point2d> pts,
                                double textHeight, double labelOffset,
                                MarkerType marker, double markerSize)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            int n = pts.Count;

            // Poligon markazi (uchlarning o'rtachasi) - raqamni tashqariga chiqarish uchun mo'ljal.
            double sx = 0.0, sy = 0.0;
            for (int i = 0; i < n; i++) { sx += pts[i].X; sy += pts[i].Y; }
            var center = new Point2d(sx / n, sy / n);

            for (int i = 0; i < n; i++)
            {
                Point2d p = pts[i];

                DrawMarker(tr, ms, p, marker, markerSize);

                Vector2d dir = OutwardDirection(pts, i, center, n);
                var lp = new Point3d(p.X + dir.X * labelOffset, p.Y + dir.Y * labelOffset, 0.0);

                var t = new DBText
                {
                    Height = textHeight,
                    TextString = (i + 1).ToString(),
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    Position = lp
                };
                t.AlignmentPoint = lp;
                ms.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
            }
        }

        private static void DrawMarker(Transaction tr, BlockTableRecord ms, Point2d p, MarkerType marker, double size)
        {
            if (marker == MarkerType.Circle)
            {
                var c = new Circle(new Point3d(p.X, p.Y, 0.0), Vector3d.ZAxis, size);
                ms.AppendEntity(c);
                tr.AddNewlyCreatedDBObject(c, true);
            }
            else if (marker == MarkerType.Cross)
            {
                AddLine(tr, ms, p.X - size, p.Y - size, p.X + size, p.Y + size);
                AddLine(tr, ms, p.X - size, p.Y + size, p.X + size, p.Y - size);
            }
            // MarkerType.None -> hech narsa chizilmaydi
        }

        /// <summary>
        /// Nuqta uchun tashqi yo'nalish: ichki burchak bissektrisasiga teskari,
        /// va poligon markazidan uzoqlashuvchi tomon tanlanadi.
        /// </summary>
        private static Vector2d OutwardDirection(IList<Point2d> pts, int i, Point2d center, int n)
        {
            if (n < 3)
            {
                Vector2d d0 = pts[i] - center;
                return d0.Length < 1e-9 ? new Vector2d(1.0, 1.0).GetNormal() : d0.GetNormal();
            }

            Point2d cur = pts[i];
            Point2d prev = pts[(i - 1 + n) % n];
            Point2d next = pts[(i + 1) % n];

            Vector2d toPrev = SafeNormal(prev - cur);
            Vector2d toNext = SafeNormal(next - cur);
            Vector2d bis = toPrev + toNext;

            Vector2d dir;
            if (bis.Length < 1e-9)
            {
                // Tekis (180°) burchak: qirraga perpendikulyar
                Vector2d edge = SafeNormal(next - prev);
                dir = new Vector2d(-edge.Y, edge.X);
            }
            else
            {
                dir = -bis.GetNormal(); // qavariq burchak uchun tashqariga qaraydi
            }

            // Poligon markazidan uzoqlashuvchi tomonni tanlaymiz (raqam ichga tushmasin).
            Vector2d fromCenter = cur - center;
            if (fromCenter.Length > 1e-9 && dir.DotProduct(fromCenter) < 0.0)
                dir = -dir;

            return dir.Length < 1e-9 ? new Vector2d(1.0, 1.0).GetNormal() : dir.GetNormal();
        }

        private static Vector2d SafeNormal(Vector2d v)
        {
            return v.Length < 1e-9 ? new Vector2d(0.0, 0.0) : v.GetNormal();
        }

        private static void AddLine(Transaction tr, BlockTableRecord ms, double x1, double y1, double x2, double y2)
        {
            var ln = new Line(new Point3d(x1, y1, 0.0), new Point3d(x2, y2, 0.0));
            ms.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
        }
    }
}
