using System.Globalization;
using System.Windows.Forms;

namespace PortraitModGenerator.Gui;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentUICulture = LocalizationManager.CurrentCulture;
        Thread.CurrentThread.CurrentUICulture = LocalizationManager.CurrentCulture;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
