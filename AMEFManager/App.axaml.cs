using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AMEFManager.Data;
using AMEFManager.Models;
using AMEFManager.Services;
using AMEFManager.ViewModels.Windows;
using AMEFManager.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AMEFManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>();
        
        services.AddScoped<AdditionalDocumentService>();
        services.AddScoped<IService<AdditionalDocument>>(x => x.GetRequiredService<AdditionalDocumentService>());
        services.AddScoped<AddressService>();
        services.AddScoped<IService<Address>>(x => x.GetRequiredService<AddressService>());
        services.AddScoped<AmefService>();
        services.AddScoped<IService<Amef>>(x => x.GetRequiredService<AmefService>());
        services.AddScoped<AuthorizationService>();
        services.AddScoped<IService<Authorization>>(x => x.GetRequiredService<AuthorizationService>());
        services.AddScoped<BillService>();
        services.AddScoped<IService<Bill>>(x => x.GetRequiredService<BillService>());
        services.AddScoped<ClientService>();
        services.AddScoped<IService<Client>>(x => x.GetRequiredService<ClientService>());
        services.AddScoped<ContractService>();
        services.AddScoped<IService<Contract>>(x => x.GetRequiredService<ContractService>());
        services.AddScoped<ContractTypeService>();
        services.AddScoped<IService<ContractType>>(x => x.GetRequiredService<ContractTypeService>());
        services.AddScoped<DeliveryDocumentService>();
        services.AddScoped<IService<DeliveryDocument>>(x => x.GetRequiredService<DeliveryDocumentService>());
        services.AddScoped<PersonService>();
        services.AddScoped<IService<Person>>(x => x.GetRequiredService<PersonService>());
        services.AddScoped<SealingDocumentService>();
        services.AddScoped<IService<SealingDocument>>(x => x.GetRequiredService<SealingDocumentService>());
        services.AddScoped<C802DocumentService>();
        services.AddScoped<IService<C802Document>>(x => x.GetRequiredService<C802DocumentService>());
        services.AddScoped<ReasonService>();
        services.AddScoped<IService<Reason>>(x => x.GetRequiredService<ReasonService>());
        
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AmefWindowViewModel>();
        services.AddTransient<AddressWindowViewModel>();
        services.AddTransient<ContractWindowViewModel>();
        services.AddTransient<PersonWindowViewModel>();
        services.AddTransient<ContractTypeWindowViewModel>();
        services.AddTransient<ClientWindowViewModel>();
        services.AddTransient<AuthorizationWindowViewModel>();
        services.AddTransient<BillWindowViewModel>();
        services.AddTransient<AdditionalDocumentWindowViewModel>();
        services.AddTransient<DeliveryDocumentWindowViewModel>();
        services.AddTransient<SealingDocumentWindowViewModel>();
        services.AddTransient<ReasonWindowViewModel>();

        services.AddSingleton<SettingsService>();
        services.AddSingleton<BackupService>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<BackupWindowViewModel>();
        services.AddTransient<ContractGenerationWindowViewModel>();
        services.AddTransient<ContractAnnexGenerationWindowViewModel>();
        services.AddTransient<InstallationDeclarationGenerationWindowViewModel>();
        services.AddTransient<AuthorizationGenerationWindowViewModel>();
        services.AddTransient<WarrantyGenerationWindowViewModel>();
        services.AddTransient<TrainingSheetGenerationWindowViewModel>();
        services.AddTransient<SealingDocumentGenerationWindowViewModel>();
        services.AddTransient<DeliveryDocumentGenerationWindowViewModel>();
        services.AddTransient<C801GenerationWindowViewModel>();
        services.AddTransient<C802GenerationWindowViewModel>();
        services.AddTransient<F4102GenerationWindowViewModel>();
        services.AddTransient<F4103GenerationWindowViewModel>();
        services.AddTransient<ContractDeadlinesViewModel>();
        services.AddTransient<AmefGprsDeadlinesViewModel>();
        services.AddTransient<DocumentGenerationService>();
        services.AddTransient<IDocumentGenerationService, DocumentGenerationService>();
        Services = services.BuildServiceProvider();
        
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
            backupService.PerformStartupBackupCheck();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}