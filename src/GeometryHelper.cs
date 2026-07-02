using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Nuqtalar ustida geometrik hisob-kitoblar: masofa, perimetr (chegara uzunligi) va yuza.
    /// </summary>
    internal static class GeometryHelper
    {
        /// <summary>Ikki nuqta orasidagi masofa.</summary>
        public static double SegmentLength(Point2d a, Point2d b)
        {
            return a.GetDistanceTo(b);
        }

        /// <summary>
        /// Chegara uzunligi (perimetr). <paramref name="closed"/> = true bo'lsa,
        /// oxirgi nuqtadan birinchisiga qaytish ham qo'shiladi.
        /// </summary>
        public static double Perimeter(IList<Point2d> pts, bool closed)
        {
            if (pts == null || pts.Count < 2) return 0.0;

            double sum = 0.0;
            for (int i = 0; i < pts.Count - 1; i++)
                sum += pts[i].GetDistanceTo(pts[i + 1]);

            if (closed && pts.Count > 2)
                sum += pts[pts.Count - 1].GetDistanceTo(pts[0]);

            return sum;
        }

        /// <summary>
        /// Yopiq ko'pburchak yuzasi (Gauss / Shoelace formulasi). Natija musbat qiymatda qaytadi.
        /// </summary>
        public static double Area(IList<Point2d> pts)
        {
            if (pts == null) return 0.0;
            int n = pts.Count;
            if (n < 3) return 0.0;

            double s = 0.0;
            for (int i = 0; i < n; i++)
            {
                Point2d p1 = pts[i];
                Point2d p2 = pts[(i + 1) % n];
                s += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return Math.Abs(s) / 2.0;
        }
    }
}
