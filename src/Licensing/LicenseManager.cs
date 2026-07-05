using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>Litsenziya tekshiruvi natijasi.</summary>
    public sealed class LicenseResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        public DateTime? ExpiryUtc { get; }

        public LicenseResult(bool isValid, string message, DateTime? expiryUtc = null)
        {
            IsValid = isValid;
            Message = message;
            ExpiryUtc = expiryUtc;
        }
    }

    /// <summary>
    /// Litsenziyani tekshiradi va o'rnatadi. Bu yerda FAQAT OCHIQ kalit bo'ladi —
    /// litsenziyani imzolash (yaratish) faqat sizdagi MAXFIY kalit bilan mumkin.
    ///
    /// Litsenziya formati:  base64url(payload) + "." + base64url(signature)
    ///   payload   = UTF8( "machineId|expiryTicksUtc" )
    ///   signature = RSA-2048 / SHA-256 / PKCS#1 v1.5
    /// </summary>
    public static class LicenseManager
    {
        // Bu loyiha (SalohiyatTable) uchun OCHIQ kalit.
        private const string PublicKeyXml =
            "<RSAKeyValue><Modulus>5C8p45egSsP42UDllvZBCr7IPCPlYY4cmShLJpQzJoXOIag2EyMQa0IbT8oHpYUosrCCGJjzrlQTBk1RDX+l7OPiAidZOgTAcmQqVbfG0x10ar/6zlPRE+9SlinieygXdXDLKYqq/syVDxmrKAw1Hc4C3+ZierVkEdp4SqKNAm+nnS/lXqD0wJdxWX8Cm1xPcSHOK+l9yQFURnkhYI/e4np7eb+hm9akVhb0j4VHATqWgVQo0sU6YX5JtpFbfUEgUfvMl7nSTWqlRF7IyIHtM1G2HrSE+n8h+yIdYn6eQh6+Er7z5cbiE2WYSo8yStakDGi9Ni0JK90TsaNnxv+SdQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>Litsenziya fayli: %APPDATA%\SalohiyatTable\license.lic</summary>
        public static string LicensePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SalohiyatTable");
                return Path.Combine(dir, "license.lic");
            }
        }

        /// <summary>Diskdagi o'rnatilgan litsenziyani o'qib tekshiradi.</summary>
        public static LicenseResult CheckInstalled()
        {
            try
            {
                if (!File.Exists(LicensePath))
                    return new LicenseResult(false, "Litsenziya o'rnatilmagan.");
                return Validate(File.ReadAllText(LicensePath));
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Litsenziyani o'qishda xato: " + ex.Message);
            }
        }

        /// <summary>Litsenziya matnini tekshiradi va yaroqli bo'lsa faylga saqlaydi.</summary>
        public static LicenseResult Install(string licenseStr)
        {
            LicenseResult result = Validate(licenseStr);
            if (result.IsValid)
            {
                try
                {
                    string dir = Path.GetDirectoryName(LicensePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(LicensePath, licenseStr.Trim());
                }
                catch (Exception ex)
                {
                    return new LicenseResult(false, "Litsenziyani saqlashda xato: " + ex.Message);
                }
            }
            return result;
        }

        /// <summary>Litsenziya matnini imzo + kompyuter + muddat bo'yicha tekshiradi.</summary>
        public static LicenseResult Validate(string licenseStr)
        {
            if (string.IsNullOrWhiteSpace(licenseStr))
                return new LicenseResult(false, "Litsenziya bo'sh.");

            try
            {
                string[] parts = licenseStr.Trim().Split('.');
                if (parts.Length != 2)
                    return new LicenseResult(false, "Litsenziya formati noto'g'ri.");

                byte[] payload = FromBase64Url(parts[0]);
                byte[] signature = FromBase64Url(parts[1]);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(PublicKeyXml);
                    if (!rsa.VerifyData(payload, CryptoConfig.MapNameToOID("SHA256"), signature))
                        return new LicenseResult(false, "Imzo yaroqsiz — litsenziya soxta yoki o'zgartirilgan.");
                }

                string text = Encoding.UTF8.GetString(payload);
                string[] fields = text.Split('|');
                if (fields.Length != 2)
                    return new LicenseResult(false, "Litsenziya ma'lumoti noto'g'ri.");

                if (!string.Equals(fields[0], MachineIdProvider.Get(), StringComparison.Ordinal))
                    return new LicenseResult(false, "Litsenziya bu kompyuter uchun emas.");

                if (!long.TryParse(fields[1], out long ticks))
                    return new LicenseResult(false, "Litsenziya muddati noto'g'ri.");

                var expiryUtc = new DateTime(ticks, DateTimeKind.Utc);
                if (DateTime.UtcNow >= expiryUtc)
                    return new LicenseResult(false, "Litsenziya muddati tugagan (" + expiryUtc.ToLocalTime() + ").", expiryUtc);

                return new LicenseResult(true, "Litsenziya yaroqli. Muddat: " + expiryUtc.ToLocalTime(), expiryUtc);
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Tekshirishda xato: " + ex.Message);
            }
        }

        private static byte[] FromBase64Url(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
