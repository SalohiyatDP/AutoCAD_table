using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Chizmada har bir nuqta yoniga tartib raqamini (va ixtiyoriy belgi aylanani) chizadi.
    /// </summary>
    internal static class MarkerDrawer
    {
        public static void Draw(Transaction tr, Database db, IList<Point2d> pts, double textHeight, double markerRadius)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            for (int i = 0; i < pts.Count; i++)
            {
                var p = new Point3d(pts[i].X, pts[i].Y, 0.0);

                if (markerRadius > 0.0)
                {
                    var c = new Circle(p, Vector3d.ZAxis, markerRadius);
                    ms.AppendEntity(c);
                    tr.AddNewlyCreatedDBObject(c, true);
                }

                var t = new DBText
                {
                    Position = new Point3d(p.X + markerRadius, p.Y + markerRadius, 0.0),
                    Height = textHeight,
                    TextString = (i + 1).ToString()
                };
                ms.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
            }
        }
    }
}
