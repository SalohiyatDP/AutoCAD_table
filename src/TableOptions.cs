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
        public double RowHeight;     // qator balandligi (matnga moslangan)
        public double HMargin;       // katakning gorizontal chekkasi (margin)
        public double VMargin;       // katakning vertikal chekkasi (margin)
        public double ColTR;         // "Nuqtalar №" ustuni kengligi
        public double ColLen;        // "Uzunligi(m)" ustuni kengligi
        public double ColX;          // "X" ustuni kengligi
        public double ColY;          // "Y" ustuni kengligi
        public double LabelOffset;   // nuqta yonidagi raqamning siljishi

        // Sonlarni formatlash (InvariantCulture bilan ishlatiladi) - 2 xonagacha
        public string CoordFormat = "0.00"; // X, Y koordinatalar
        public string LenFormat = "0.00";   // masofa / chegara uzunligi
        public string AreaFormat = "0.00";  // yuza (m.kv)
        public string HaFormat = "0.####";  // yuza (gektar) - aniqlik uchun ko'proq xona

        /// <summary>Matn balandligiga qarab standart sozlamalarni hisoblaydi.</summary>
        public static TableOptions FromTextHeight(double th)
        {
            if (th <= 0) th = 2.5;
            return new TableOptions
            {
                TextHeight = th,
                VMargin = th * 0.3,
                HMargin = th * 0.5,
                // Qator balandligi = matn + yuqori/quyi chekka -> matnga aniq moslashadi
                RowHeight = th + (th * 0.3) * 2.0,
                ColTR = th * 9.0,
                ColLen = th * 11.0,
                ColX = th * 12.0,
                ColY = th * 12.0,
                LabelOffset = th * 0.5
            };
        }
    }
}
