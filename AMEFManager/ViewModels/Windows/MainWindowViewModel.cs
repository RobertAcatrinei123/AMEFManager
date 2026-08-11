using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AMEFManager.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] 
    private object? _currentPage;

    public MainWindowViewModel()
    {
        _currentPage = null;
        
        _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var githubUrl = "https://github.com/RobertAcatrinei123/AMEFManager"; 
            var source = new Velopack.Sources.GithubSource(githubUrl, null, false);
            var mgr = new Velopack.UpdateManager(source);

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return;
            }

            await mgr.DownloadUpdatesAsync(newVersion);

            mgr.WaitExitThenApplyUpdates(newVersion);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
        }
    }
    
    private Microsoft.Extensions.DependencyInjection.IServiceScope? _currentScope;

    [RelayCommand]
    private void Navigate(string destination)
    {
        _currentScope?.Dispose();
        _currentScope = App.Services.CreateScope();
        
        CurrentPage = destination switch
        {
            "AdditionalDocuments" => _currentScope.ServiceProvider.GetRequiredService<AdditionalDocumentWindowViewModel>(),
            "Addresses" => _currentScope.ServiceProvider.GetRequiredService<AddressWindowViewModel>(),
            "Amef" => _currentScope.ServiceProvider.GetRequiredService<AmefWindowViewModel>(),
            "Authorizations" => _currentScope.ServiceProvider.GetRequiredService<AuthorizationWindowViewModel>(),
            "Bills" => _currentScope.ServiceProvider.GetRequiredService<BillWindowViewModel>(),
            "Clients" => _currentScope.ServiceProvider.GetRequiredService<ClientWindowViewModel>(),
            "Contracts" => _currentScope.ServiceProvider.GetRequiredService<ContractWindowViewModel>(),
            "ContractTypes" => _currentScope.ServiceProvider.GetRequiredService<ContractTypeWindowViewModel>(),
            "DeliveryDocuments" => _currentScope.ServiceProvider.GetRequiredService<DeliveryDocumentWindowViewModel>(),
            "People" => _currentScope.ServiceProvider.GetRequiredService<PersonWindowViewModel>(),
            "SealingDocuments" => _currentScope.ServiceProvider.GetRequiredService<SealingDocumentWindowViewModel>(),
            "Settings" => _currentScope.ServiceProvider.GetRequiredService<SettingsWindowViewModel>(),
            "Backup" => _currentScope.ServiceProvider.GetRequiredService<BackupWindowViewModel>(),
            _ => throw new ArgumentException("Invalid navigation target")
        };
    }
    [RelayCommand]
    private void OpenAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = System.IO.Path.Combine(folder, "AMEFManager");
        if (System.IO.Directory.Exists(path))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }
    }
}