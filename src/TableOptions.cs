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
        public double RowHeight;     // qator balandligi (matnga aniq moslangan)
        public double ColTR;         // "Nuqtalar №" ustuni kengligi
        public double ColLen;        // "Uzunligi(m)" ustuni kengligi
        public double ColX;          // "X" ustuni kengligi
        public double ColY;          // "Y" ustuni kengligi
        public double LabelOffset;   // nuqta yonidagi raqamning siljishi
        public double MarkerSize;    // nuqta belgisi (doira radiusi / X yarim o'lchami)

        // Sonlarni formatlash (InvariantCulture bilan) - hammasi 2 xonagacha
        public string CoordFormat = "0.00"; // X, Y koordinatalar
        public string LenFormat = "0.00";   // masofa / chegara uzunligi
        public string AreaFormat = "0.00";  // yuza (m.kv)
        public string HaFormat = "0.00";    // yuza (gektar)

        /// <summary>Matn balandligiga qarab standart sozlamalarni hisoblaydi.</summary>
        public static TableOptions FromTextHeight(double th)
        {
            if (th <= 0) th = 2.5;
            return new TableOptions
            {
                TextHeight = th,
                RowHeight = th * 1.8,   // matn + kichik bo'shliq
                ColTR = th * 8.0,
                ColLen = th * 10.0,
                ColX = th * 13.0,
                ColY = th * 13.0,
                LabelOffset = th * 1.2,
                MarkerSize = th * 0.5
            };
        }
    }
}
