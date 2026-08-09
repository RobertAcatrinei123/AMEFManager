using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace AMEFManager.Helpers;

public static class MessageBox
{
    public static Task ShowError(string message, string title = "Eroare")
    {
        return Show(message, title);
    }

    public static Task ShowInfo(string message, string title = "Informatie")
    {
        return Show(message, title);
    }

    public static Task ShowWarning(string message, string title = "Atentie")
    {
        return Show(message, title);
    }

    private static async Task Show(string message, string title)
    {
        var dialog = new MessageBoxWindow(message, title);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            await dialog.ShowDialog(desktop.MainWindow);
        }
    }
}
