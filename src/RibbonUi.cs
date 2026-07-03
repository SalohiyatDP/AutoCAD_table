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

            // Chegaradoshlar jadvali paneli
            var sourceN = new RibbonPanelSource { Title = "Chegaradoshlar" };
            var panelN = new RibbonPanel { Source = sourceN };
            tab.Panels.Add(panelN);

            sourceN.Items.Add(CreateButton(
                "Chegaradoshlar\n(nuqtalardan)", "PTCHEGARA",
                "Nuqtalardan chegaradoshlar jadvali (faqat raqamlar)"));

            sourceN.Items.Add(new RibbonSeparator());

            sourceN.Items.Add(CreateButton(
                "Chegaradoshlar\n(poliliniya)", "PLCHEGARA",
                "Poliliniyadan chegaradoshlar jadvali (faqat raqamlar)"));

            // Poligon paneli
            var sourceP = new RibbonPanelSource { Title = "Poligon" };
            var panelP = new RibbonPanel { Source = sourceP };
            tab.Panels.Add(panelP);

            sourceP.Items.Add(CreateButton(
                "Nuqtalardan\npoligon yaratish", "PTPOLIGON",
                "GPS nuqta raqamlari (masalan 145-165, 171, 182-260) bo'yicha poligon yasash"));

            sourceP.Items.Add(new RibbonSeparator());

            sourceP.Items.Add(CreateButton(
                "Tomorqa\nyaratish", "PTTOMORQA",
                "Yopiq maydondan ichkariga surilgan (sozlamadagi masofa) 'Tomorqa' poligonini yasash"));

            // Devor paneli
            var sourceD = new RibbonPanelSource { Title = "Devor" };
            var panelD = new RibbonPanel { Source = sourceD };
            tab.Panels.Add(panelD);

            sourceD.Items.Add(CreateButton(
                "Devor\nbelgilash", "PTDEVOR",
                "Devor bo'ylab poliliniya chizib, sozlamadagi chiziq turi va masshtab bilan devor belgisi yasash"));

            // Ikkinchi panel: Sozlamalar va Haqida
            var source2 = new RibbonPanelSource { Title = "Sozlamalar" };
            var panel2 = new RibbonPanel { Source = source2 };
            tab.Panels.Add(panel2);

            source2.Items.Add(CreateButton(
                "Sozlamalar", "PTSOZLAMA",
                "Matn balandligi, o'nlik xonalar va nuqta belgisini sozlash (saqlanadi)"));

            source2.Items.Add(new RibbonSeparator());

            source2.Items.Add(CreateButton(
                "Haqida", "PTHAQIDA",
                "Plagin va mualliflar haqida ma'lumot"));

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
