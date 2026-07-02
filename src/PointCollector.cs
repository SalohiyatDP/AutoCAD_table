using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Foydalanuvchidan nuqtalarni yig'ish usullari: interaktiv ko'rsatish yoki
    /// mavjud poliliniyadan cho'qqilarni olish.
    /// </summary>
    internal static class PointCollector
    {
        /// <summary>
        /// Foydalanuvchi sichqoncha bilan nuqtalarni ketma-ket ko'rsatadi.
        /// Tugatish uchun Enter/Escape bosiladi. Nuqtalar tanlangan tartibda qaytadi.
        /// </summary>
        public static List<Point2d> PickPoints(Editor ed)
        {
            var pts = new List<Point2d>();

            while (true)
            {
                var ppo = new PromptPointOptions(
                    pts.Count == 0
                        ? "\n1-nuqtani ko'rsating (tugatish uchun Enter): "
                        : "\n" + (pts.Count + 1) + "-nuqtani ko'rsating (tugatish uchun Enter): ");
                ppo.AllowNone = true; // Enter -> None

                if (pts.Count > 0)
                {
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = new Point3d(pts[pts.Count - 1].X, pts[pts.Count - 1].Y, 0.0);
                    ppo.UseDashedLine = true;
                }

                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                    pts.Add(new Point2d(ppr.Value.X, ppr.Value.Y));
                else
                    break; // Enter yoki Escape
            }

            return pts;
        }

        /// <summary>
        /// LWPOLYLINE tanlab, uning cho'qqilarini tartib bilan qaytaradi.
        /// Yopiq poliliniyada birinchi va oxirgi cho'qqi ustma-ust tushsa, dublikat olib tashlanadi.
        /// </summary>
        public static List<Point2d> FromPolyline(Editor ed, Transaction tr)
        {
            var peo = new PromptEntityOptions("\nPoliliniyani tanlang: ");
            peo.SetRejectMessage("\nFaqat oddiy (LWPOLYLINE) poliliniya tanlang.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return null;

            var pl = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);
            var pts = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
                pts.Add(pl.GetPoint2dAt(i));

            if (pts.Count > 1 && pts[0].GetDistanceTo(pts[pts.Count - 1]) < 1e-9)
                pts.RemoveAt(pts.Count - 1);

            return pts;
        }
    }
}
