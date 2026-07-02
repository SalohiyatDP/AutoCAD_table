namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Jadval va yozuvlarning o'lchov/format sozlamalari.
    /// Barcha o'lchamlar chizma birligida (odatda metr) beriladi va matn balandligiga
    /// nisbatan proporsional hisoblanadi, shunda jadval turli masshtablarga moslashadi.
    /// </summary>
    internal sealed class TableOptions
    {
        // O'lchamlar (chizma birligida)
        public double TextHeight;    // matn balandligi
        public double RowHeight;     // qator balandligi
        public double ColTR;         // "Nuqtalar T/R" ustuni kengligi
        public double ColLen;        // "Uzunligi(m)" ustuni kengligi
        public double ColX;          // "X" ustuni kengligi
        public double ColY;          // "Y" ustuni kengligi
        public double MarkerRadius;  // nuqta belgisi (aylana) radiusi

        // Sonlarni formatlash (InvariantCulture bilan ishlatiladi)
        public string CoordFormat = "0.###"; // X, Y koordinatalar
        public string LenFormat = "0.##";     // masofa / chegara uzunligi
        public string AreaFormat = "0.##";     // yuza (m.kv)
        public string HaFormat = "0.####";     // yuza (gektar)

        /// <summary>Matn balandligiga qarab standart sozlamalarni hisoblaydi.</summary>
        public static TableOptions FromTextHeight(double th)
        {
            if (th <= 0) th = 2.5;
            return new TableOptions
            {
                TextHeight = th,
                RowHeight = th * 2.2,
                ColTR = th * 8.0,
                ColLen = th * 11.0,
                ColX = th * 11.0,
                ColY = th * 11.0,
                MarkerRadius = th * 0.6
            };
        }
    }
}
