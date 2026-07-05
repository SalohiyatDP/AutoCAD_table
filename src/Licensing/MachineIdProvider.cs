using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Joriy kompyuter uchun barqaror identifikator (CPU ProcessorId + BIOS seriya raqami
    /// asosida SHA-256). Foydalanuvchi bu ID ni sizga (vendorga) yuboradi, siz esa unga
    /// mos litsenziya imzolaysiz.
    /// </summary>
    public static class MachineIdProvider
    {
        public static string Get()
        {
            string cpu = QueryWmi("Win32_Processor", "ProcessorId");
            string bios = QueryWmi("Win32_BIOS", "SerialNumber");
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(cpu + "|" + bios));
                return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        private static string QueryWmi(string wmiClass, string property)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT " + property + " FROM " + wmiClass))
                {
                    foreach (ManagementBaseObject mo in searcher.Get())
                    {
                        object v = mo[property];
                        if (v != null) return v.ToString().Trim();
                    }
                }
            }
            catch { /* WMI mavjud bo'lmasa bo'sh qaytadi */ }
            return "";
        }
    }
}
