using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Litsenziya kodlash yordamchilari: Base32 (RFC 4648 alifbosi) + GZip.
    /// Vendor vositasi (Web Crypto + CompressionStream) bilan bir xil format:
    ///   machineId  = Base32(GZip(UTF8(ProcessorId)))
    ///   license    = Base32(GZip(UTF8("ProcessorId|ticks") + 256-baytli RSA imzo))
    /// </summary>
    internal static class LicenseCodec
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Base32Encode(byte[] data)
        {
            var sb = new StringBuilder();
            int bits = 0, value = 0;
            foreach (byte b in data)
            {
                value = (value << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0)
                sb.Append(Alphabet[(value << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] Base32Decode(string input)
        {
            input = input.ToUpperInvariant();
            using (var ms = new MemoryStream())
            {
                int bits = 0, value = 0;
                foreach (char c in input)
                {
                    if (c == '-') continue;
                    int idx = Alphabet.IndexOf(c);
                    if (idx < 0) throw new FormatException("Yaroqsiz Base32 belgi: " + c);
                    value = (value << 5) | idx;
                    bits += 5;
                    if (bits >= 8)
                    {
                        ms.WriteByte((byte)((value >> (bits - 8)) & 0xFF));
                        bits -= 8;
                    }
                }
                return ms.ToArray();
            }
        }

        public static byte[] Compress(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress))
                    gz.Write(data, 0, data.Length);
                return ms.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
