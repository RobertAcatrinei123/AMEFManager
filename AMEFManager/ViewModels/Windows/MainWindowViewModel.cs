using System;
using System.Linq;
using AMEFManager.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AMEFManager.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private IServiceScope? _currentScope;
    private bool _disposed;

    [ObservableProperty] 
    private object? _currentPage;

    public MainWindowViewModel(IServiceScopeFactory? scopeFactory = null)
    {
        _scopeFactory = scopeFactory;
        _currentPage = null;
        
        _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                a.GetName().Name?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true ||
                a.GetName().Name?.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) == true))
            {
                return;
            }

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

            var versionStr = newVersion.TargetFullRelease?.Version?.ToString() ?? "nouă";
            AppLogger.LogInfo($"New update available: {versionStr}. Downloading updates...");

            await MessageBox.ShowInfo(
                $"O nouă versiune a aplicației este disponibilă ({versionStr}).\nActualizarea se va descărca în fundal.",
                "Actualizare Disponibilă");

            await mgr.DownloadUpdatesAsync(newVersion);

            AppLogger.LogInfo("Applying updates and preparing restart...");

            await MessageBox.ShowInfo(
                $"Versiunea {versionStr} a fost descărcată cu succes.\nAplicația se va reporni pentru a aplica modificările.",
                "Actualizare Pregătită");

            mgr.WaitExitThenApplyUpdates(newVersion);
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Update check/apply failed: {ex.Message}", ex);
        }
    }

    [RelayCommand]
    private void Navigate(string destination)
    {
        AppLogger.LogInfo($"Navigating to '{destination}'");
        (CurrentPage as IDisposable)?.Dispose();
        _currentScope?.Dispose();

        var factory = _scopeFactory ?? App.Services?.GetService<IServiceScopeFactory>();
        _currentScope = factory?.CreateScope() ?? App.Services?.CreateScope();
        if (_currentScope == null) return;
        
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

    public static Action<string>? FolderOpener { get; set; }

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
                if (FolderOpener != null)
                {
                    FolderOpener(path);
                    return;
                }

                // Suppress GUI launch during unit tests
                if (AppDomain.CurrentDomain.GetAssemblies().Any(a => 
                    a.GetName().Name?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true || 
                    a.GetName().Name?.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) == true))
                {
                    AppLogger.LogDebug($"[MainWindowViewModel] Suppressing live GUI process launch during test execution for: {path}");
                    return;
                }

                if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        (CurrentPage as IDisposable)?.Dispose();
        CurrentPage = null;

        _currentScope?.Dispose();
        _currentScope = null;
    }
}