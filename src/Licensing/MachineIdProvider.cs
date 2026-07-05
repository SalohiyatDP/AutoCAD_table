using System.Management;
using System.Text;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Joriy kompyuter identifikatori (Product key). Topography bilan bir xil usul:
    ///   Machine ID = Base32(GZip(UTF8(ProcessorId)))
    /// </summary>
    public static class MachineIdProvider
    {
        public static string Get()
        {
            string processorId = GetProcessorId();
            return LicenseCodec.Base32Encode(LicenseCodec.Compress(Encoding.UTF8.GetBytes(processorId)));
        }

        public static string GetProcessorId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementBaseObject mo in searcher.Get())
                    {
                        object v = mo["ProcessorId"];
                        if (v != null) return v.ToString().Trim();
                    }
                }
            }
            catch { /* WMI mavjud bo'lmasa bo'sh */ }
            return string.Empty;
        }
    }
}
