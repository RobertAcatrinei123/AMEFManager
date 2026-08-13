using System;
using AMEFManager.Helpers;
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
            AppLogger.LogInfo("Checking for Velopack application updates...");
            var githubUrl = "https://github.com/RobertAcatrinei123/AMEFManager"; 
            var source = new Velopack.Sources.GithubSource(githubUrl, null, false);
            var mgr = new Velopack.UpdateManager(source);

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                AppLogger.LogInfo("Application is up to date.");
                return;
            }

            AppLogger.LogInfo($"New update available: {newVersion.TargetFullRelease.Version}. Downloading updates...");
            await mgr.DownloadUpdatesAsync(newVersion);

            AppLogger.LogInfo("Applying updates and preparing restart...");
            mgr.WaitExitThenApplyUpdates(newVersion);
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Update check/apply failed: {ex.Message}", ex);
        }
    }
    
    private Microsoft.Extensions.DependencyInjection.IServiceScope? _currentScope;

    [RelayCommand]
    private void Navigate(string destination)
    {
        AppLogger.LogInfo($"Navigating to '{destination}'");
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
            "Reasons" => _currentScope.ServiceProvider.GetRequiredService<ReasonWindowViewModel>(),
            "Settings" => _currentScope.ServiceProvider.GetRequiredService<SettingsWindowViewModel>(),
            "Backup" => _currentScope.ServiceProvider.GetRequiredService<BackupWindowViewModel>(),
            "C801" => _currentScope.ServiceProvider.GetRequiredService<C801GenerationWindowViewModel>(),
            "C802" => _currentScope.ServiceProvider.GetRequiredService<C802GenerationWindowViewModel>(),
            "F4102" => _currentScope.ServiceProvider.GetRequiredService<F4102GenerationWindowViewModel>(),
            "F4103" => _currentScope.ServiceProvider.GetRequiredService<F4103GenerationWindowViewModel>(),
            "ContractGeneration" => _currentScope.ServiceProvider.GetRequiredService<ContractGenerationWindowViewModel>(),
            "ContractAnnexGeneration" => _currentScope.ServiceProvider.GetRequiredService<ContractAnnexGenerationWindowViewModel>(),
            "InstallationDeclarationGeneration" => _currentScope.ServiceProvider.GetRequiredService<InstallationDeclarationGenerationWindowViewModel>(),
            "AuthorizationGeneration" => _currentScope.ServiceProvider.GetRequiredService<AuthorizationGenerationWindowViewModel>(),
            "WarrantyGeneration" => _currentScope.ServiceProvider.GetRequiredService<WarrantyGenerationWindowViewModel>(),
            "TrainingSheetGeneration" => _currentScope.ServiceProvider.GetRequiredService<TrainingSheetGenerationWindowViewModel>(),
            "SealingDocumentGeneration" => _currentScope.ServiceProvider.GetRequiredService<SealingDocumentGenerationWindowViewModel>(),
            "DeliveryDocumentGeneration" => _currentScope.ServiceProvider.GetRequiredService<DeliveryDocumentGenerationWindowViewModel>(),
            _ => throw new ArgumentException("Invalid navigation target")
        };
    }

    [RelayCommand]
    private void OpenAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = System.IO.Path.Combine(folder, "AMEFManager");
        AppLogger.LogInfo($"Opening AppData folder: '{path}'");
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
                AppLogger.LogError($"Failed to open folder '{path}': {ex.Message}", ex);
            }
        }
        else
        {
            AppLogger.LogWarning($"AppData folder does not exist at '{path}'");
        }
    }
}