using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace AMEFManager.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}

public class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                return;
            }

            foreach (var window in desktop.Windows)
            {
                if (window?.Clipboard != null)
                {
                    await window.Clipboard.SetTextAsync(text);
                    return;
                }
            }
        }
    }
}
