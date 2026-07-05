using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Buyruq boshida chaqiriladigan "darvoza". Litsenziya yaroqli bo'lsa true,
    /// aks holda AutoCAD buyruq satriga xabar yozib false qaytaradi.
    /// </summary>
    public static class LicenseGate
    {
        public static bool Ensure()
        {
            LicenseResult result = LicenseManager.CheckInstalled();
            if (!result.IsValid)
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage(
                        "\n[SalohiyatTable litsenziya] " + result.Message +
                        "\n  Machine ID ni ko'rish:  PTID" +
                        "\n  Litsenziyani o'rnatish: PTLIC\n");
                }
                return false;
            }
            return true;
        }
    }
}
