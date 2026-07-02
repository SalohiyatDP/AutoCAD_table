#if RIBBON
using System;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(SalohiyatDP.AutoCADTable.PluginApp))]

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Plagin yuklanganda ishga tushadi va lentaga (Ribbon) "SalohiyatTable"
    /// yorlig'ini (tab) hamda buyruq tugmalarini qo'shadi.
    ///
    /// Eslatma: bu klass faqat AdWindows.dll topilganda (RIBBON belgisi bilan)
    /// kompilyatsiya qilinadi. Aks holda plagin faqat PTABLE/PLTABLE buyruqlarini beradi.
    /// </summary>
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            // Lenta hali tayyor bo'lmasligi mumkin -> tayyor bo'lguncha kutamiz.
            if (ComponentManager.Ribbon != null)
            {
                RibbonBuilder.Build();
            }
            else
            {
                ComponentManager.ItemInitialized += OnItemInitialized;
            }
        }

        private void OnItemInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon != null)
            {
                ComponentManager.ItemInitialized -= OnItemInitialized;
                RibbonBuilder.Build();
            }
        }

        public void Terminate()
        {
        }
    }

    internal static class RibbonBuilder
    {
        private const string TabId = "SALOHIYAT_TABLE_TAB";

        public static void Build()
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;

            // Takroriy qo'shilishning oldini olish
            foreach (RibbonTab t in ribbon.Tabs)
                if (t.Id == TabId) return;

            var tab = new RibbonTab { Title = "SalohiyatTable", Id = TabId };
            ribbon.Tabs.Add(tab);

            var source = new RibbonPanelSource { Title = "Jadval" };
            var panel = new RibbonPanel { Source = source };
            tab.Panels.Add(panel);

            source.Items.Add(CreateButton(
                "Nuqtalardan\njadval", "PTABLE",
                "Nuqtalarni ketma-ket ko'rsatib koordinata jadvali yasash"));

            source.Items.Add(new RibbonSeparator());

            source.Items.Add(CreateButton(
                "Poliliniyadan\njadval", "PLTABLE",
                "Poliliniya (LWPOLYLINE) cho'qqilaridan koordinata jadvali yasash"));

            // Lentani yaratilgan yorliqqa o'tkazish (ixtiyoriy)
            tab.IsActive = true;
        }

        private static RibbonButton CreateButton(string text, string command, string tooltip)
        {
            return new RibbonButton
            {
                Text = text,
                ShowText = true,
                ShowImage = false,
                Size = RibbonItemSize.Large,
                Orientation = Orientation.Vertical,
                CommandParameter = command,
                ToolTip = tooltip,
                CommandHandler = new RibbonCommandHandler()
            };
        }
    }

    /// <summary>Tugma bosilganda tegishli AutoCAD buyrug'ini ishga tushiradi.</summary>
    internal class RibbonCommandHandler : ICommand
    {
        public bool CanExecute(object parameter) => true;

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public void Execute(object parameter)
        {
            var button = parameter as RibbonButton;
            if (button == null) return;

            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            string command = "_." + button.CommandParameter + " ";
            doc.SendStringToExecute(command, true, false, true);
        }
    }
}
#endif
