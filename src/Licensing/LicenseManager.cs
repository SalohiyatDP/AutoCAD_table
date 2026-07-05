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
    /// Litsenziyani tekshiradi va o'rnatadi. Topography plagini bilan AYNAN BIR XIL usul:
    ///   license = Base32( GZip( UTF8("ProcessorId|expiryTicks") + 256-baytli RSA imzo ) )
    /// Bu yerda FAQAT OCHIQ kalit bo'ladi — imzolash sizdagi MAXFIY kalit bilan qilinadi.
    /// </summary>
    public static class LicenseManager
    {
        // SalohiyatTable uchun OCHIQ kalit.
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

        public static LicenseResult Install(string licenseStr)
        {
            LicenseResult result = Validate(licenseStr);
            if (result.IsValid)
            {
                try
                {
                    string dir = Path.GetDirectoryName(LicensePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(LicensePath, (licenseStr ?? string.Empty).Trim());
                }
                catch (Exception ex)
                {
                    return new LicenseResult(false, "Litsenziyani saqlashda xato: " + ex.Message);
                }
            }
            return result;
        }

        public static LicenseResult Validate(string licenseStr)
        {
            if (string.IsNullOrWhiteSpace(licenseStr))
                return new LicenseResult(false, "Litsenziya bo'sh.");

            try
            {
                byte[] all = LicenseCodec.Decompress(LicenseCodec.Base32Decode(licenseStr.Trim()));
                if (all.Length <= 256)
                    return new LicenseResult(false, "Litsenziya formati noto'g'ri.");

                byte[] payload = new byte[all.Length - 256];
                byte[] signature = new byte[256];
                Buffer.BlockCopy(all, 0, payload, 0, payload.Length);
                Buffer.BlockCopy(all, payload.Length, signature, 0, 256);

                string text = Encoding.UTF8.GetString(payload);
                string[] parts = text.Split('|');
                if (parts.Length != 2)
                    return new LicenseResult(false, "Litsenziya ma'lumoti noto'g'ri.");

                // Kompyuterга bog'lanish: payload ichidagi ProcessorId dan Machine ID qayta hisoblanadi.
                string recomputedId = LicenseCodec.Base32Encode(
                    LicenseCodec.Compress(Encoding.UTF8.GetBytes(parts[0])));
                if (!string.Equals(recomputedId, MachineIdProvider.Get(), StringComparison.Ordinal))
                    return new LicenseResult(false, "Litsenziya bu kompyuter uchun emas.");

                if (!long.TryParse(parts[1], out long ticks))
                    return new LicenseResult(false, "Litsenziya muddati noto'g'ri.");

                var expiryUtc = new DateTime(ticks, DateTimeKind.Utc);
                if (DateTime.UtcNow >= expiryUtc)
                    return new LicenseResult(false, "Litsenziya muddati tugagan (" + expiryUtc.ToLocalTime() + ").", expiryUtc);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(PublicKeyXml);
                    bool ok = rsa.VerifyData(Encoding.UTF8.GetBytes(text), CryptoConfig.MapNameToOID("SHA256"), signature);
                    if (!ok)
                        return new LicenseResult(false, "Imzo yaroqsiz — litsenziya soxta yoki o'zgartirilgan.");
                }

                return new LicenseResult(true, "Litsenziya yaroqli. Muddat: " + expiryUtc.ToLocalTime(), expiryUtc);
            }
            catch (Exception ex)
            {
                return new LicenseResult(false, "Tekshirishda xato: " + ex.Message);
            }
        }
    }
}
