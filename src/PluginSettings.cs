using System;
using System.IO;
using System.Xml.Serialization;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>Nuqta ustiga qo'yiladigan belgi turi.</summary>
    public enum MarkerType
    {
        None,   // hech narsa
        Circle, // doira
        Cross   // X belgisi
    }

    /// <summary>
    /// Ko'rsatilgan nuqta jadvalning qaysi burchagi bo'lishini bildiradi.
    /// (Tartib: SettingsForm dagi ro'yxat tartibiga mos.)
    /// </summary>
    public enum TableAnchor
    {
        TopLeft,     // chap-yuqori
        TopRight,    // o'ng-yuqori
        BottomLeft,  // chap-pastki
        BottomRight  // o'ng-pastki
    }

    /// <summary>Tomorqa poligonining burchak ko'rinishi.</summary>
    public enum TomorqaCornerStyle
    {
        Sharp, // qirrali (o'tkir burchak)
        Arc    // yoysimon (yumaloqlangan)
    }

    /// <summary>
    /// Plagin haqida ma'lumot (menyudagi "Haqida" bo'limi uchun).
    /// </summary>
    public static class PluginInfo
    {
        public const string Name = "SalohiyatTable";
        public const string Version = "1.0";
        public const string AuthorsTitle = "Mualliflar";
        public const string Author1 = "Topograf : Abdujabborov Sherzod Jahongir o'g'li";
        public const string Author2 = "Topograf : Karimbekov Asadbek Nasibbek o'g'li";
        public const string Organization = "Tashkilot: Davlat Kadastrlari Palatasi, Kosonsoy tuman filiali";
    }

    /// <summary>
    /// Plagin sozlamalari. %APPDATA%\SalohiyatTable\settings.xml faylida saqlanadi,
    /// shuning uchun AutoCAD qayta ochilganda ham eslab qolinadi.
    /// </summary>
    [XmlRoot("SalohiyatTableSettings")]
    public class PluginSettings
    {
        public double TextHeight { get; set; } = 2.5;
        public MarkerType Marker { get; set; } = MarkerType.None;
        public int Decimals { get; set; } = 2;
        public TableAnchor Anchor { get; set; } = TableAnchor.TopLeft;
        public TableAnchor NeighborsAnchor { get; set; } = TableAnchor.TopLeft;

        // Chegaradoshlar jadvali uchun ijrochi ma'lumoti
        public string Ijrochi { get; set; } = "Karimbekov Asadbek Nasibbek o'g'li";

        // Chegaradoshlar jadvali uchun alohida matn balandligi
        public double NeighborsTextHeight { get; set; } = 2.5;

        // Chegaradoshlar ("Chegaradoshlar" ustuni) kengligi - matn balandligiga nisbatan
        public double NeighborsColWidthFactor { get; set; } = 16.0;

        // Tomorqa yeri funksiyasi uchun
        public double TomorqaOffset { get; set; } = 2.0;       // tashqi chegaradan ichkariga (metr)
        public double TomorqaTextHeight { get; set; } = 2.5;   // "Tomorqa" yozuvi balandligi
        public TomorqaCornerStyle TomorqaCorner { get; set; } = TomorqaCornerStyle.Sharp; // burchak turi
        public double TomorqaArcRadius { get; set; } = 2.0;    // yoysimon burchak radiusi
        public double TomorqaLtScale { get; set; } = 1.0;      // nuqtali chiziq masshtabi (LTSCALE)

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SalohiyatTable");
                return Path.Combine(dir, "settings.xml");
            }
        }

        /// <summary>Saqlangan sozlamalarni yuklaydi; bo'lmasa standart qiymatlar.</summary>
        public static PluginSettings Load()
        {
            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    var ser = new XmlSerializer(typeof(PluginSettings));
                    using (var fs = File.OpenRead(path))
                        return (PluginSettings)ser.Deserialize(fs);
                }
            }
            catch
            {
                // Buzilgan/eski fayl bo'lsa - standart qiymatlarga qaytamiz.
            }
            return new PluginSettings();
        }

        /// <summary>Sozlamalarni diskka saqlaydi.</summary>
        public void Save()
        {
            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var ser = new XmlSerializer(typeof(PluginSettings));
                using (var fs = File.Create(path))
                    ser.Serialize(fs, this);
            }
            catch
            {
                // Yozib bo'lmasa - jimgina o'tkazamiz.
            }
        }

        /// <summary>O'nlik xonalar soniga ko'ra format satri ("0.00" kabi).</summary>
        public string NumberFormat()
        {
            int d = Decimals < 0 ? 0 : Decimals;
            return d == 0 ? "0" : "0." + new string('0', d);
        }
    }
}
