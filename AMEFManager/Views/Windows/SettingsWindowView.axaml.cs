using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AMEFManager.ViewModels.Windows;

namespace AMEFManager.Views.Windows;

public partial class SettingsWindowView : UserControl
{
    public SettingsWindowView()
    {
        InitializeComponent();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selectati",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            if (DataContext is SettingsWindowViewModel vm)
            {
                vm.ServerPath = folders[0].Path.LocalPath;
            }
        }
    }
}
