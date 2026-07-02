using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Chizmada har bir nuqta yoniga faqat tartib raqamini yozadi (doira chizilmaydi).
    /// </summary>
    internal static class MarkerDrawer
    {
        public static void Draw(Transaction tr, Database db, IList<Point2d> pts, double textHeight, double labelOffset)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            for (int i = 0; i < pts.Count; i++)
            {
                var t = new DBText
                {
                    Position = new Point3d(pts[i].X + labelOffset, pts[i].Y + labelOffset, 0.0),
                    Height = textHeight,
                    TextString = (i + 1).ToString()
                };
                ms.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
            }
        }
    }
}
