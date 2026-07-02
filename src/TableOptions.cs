namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Jadval va yozuvlarning o'lchov/format sozlamalari. Barcha o'lchamlar chizma
    /// birligida (odatda metr) beriladi va matn balandligiga proporsional hisoblanadi.
    /// </summary>
    internal sealed class TableOptions
    {
        // O'lchamlar (chizma birligida)
        public double TextHeight;    // matn balandligi
        public double RowHeight;     // qator balandligi (matnga aniq moslangan)
        public double LabelOffset;   // nuqta yonidagi raqamning siljishi
        public double MarkerSize;    // nuqta belgisi (doira radiusi / X yarim o'lchami)

        // Ustun kengligini matnga moslash uchun (auto-fit)
        public double CharWidth;     // bitta belgi taxminiy kengligi
        public double ColPadding;    // ustun ichidagi chap/o'ng bo'shliq
        public double MinColWidth;   // ustunning minimal kengligi

        public MarkerType Marker;
        public TableAnchor Anchor;

        // Sonlarni formatlash (InvariantCulture bilan)
        public string CoordFormat = "0.00";
        public string LenFormat = "0.00";
        public string AreaFormat = "0.00";
        public string HaFormat = "0.00";

        /// <summary>Saqlangan sozlamalardan TableOptions yasaydi.</summary>
        public static TableOptions FromSettings(PluginSettings s)
        {
            double th = (s.TextHeight <= 0) ? 2.5 : s.TextHeight;
            string fmt = s.NumberFormat();

            return new TableOptions
            {
                TextHeight = th,
                RowHeight = th * 1.8,
                LabelOffset = th * 1.2,
                MarkerSize = th * 0.5,
                CharWidth = th * 0.75,
                ColPadding = th * 0.8,
                MinColWidth = th * 4.0,
                Marker = s.Marker,
                Anchor = s.Anchor,
                CoordFormat = fmt,
                LenFormat = fmt,
                AreaFormat = fmt,
                HaFormat = fmt
            };
        }
    }
}
