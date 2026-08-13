using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using AMEFManager.Models;

namespace AMEFManager.Data;

public class AppDbContext : DbContext
{
    public DbSet<AdditionalDocument> AdditionalDocuments { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Amef> Amefs { get; set; }
    public DbSet<Authorization> Authorizations { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractType> ContractTypes { get; set; }
    public DbSet<DeliveryDocument> DeliveryDocuments { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<SealingDocument> SealingDocuments { get; set; }
    public DbSet<C802Document> C802Documents { get; set; }
    public DbSet<Reason> Reasons { get; set; }

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(folder, "AMEFManager");
            
            Directory.CreateDirectory(path);
        
            var dbPath = Path.Combine(path, "storage.sqlite");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateOnly>()
            .HaveConversion<DateOnlyConverter>();

        configurationBuilder.Properties<DateOnly?>()
            .HaveConversion<NullableDateOnlyConverter>();

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<DateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<NullableDateTimeConverter>();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Amef>()
            .HasIndex(a => a.Series)
            .IsUnique(); 
        
        modelBuilder.Entity<Bill>()
            .HasIndex(a => new {a.BillSeries, a.BillNumber})
            .IsUnique();
        
        modelBuilder.Entity<Client>()
            .HasIndex(a => a.NationalIdentifier)
            .IsUnique();
        
        modelBuilder.Entity<Contract>()
            .HasIndex(a => a.Number)
            .IsUnique();
        
        modelBuilder.Entity<DeliveryDocument>()
            .HasIndex(a => a.Number)
            .IsUnique();
        
        modelBuilder.Entity<SealingDocument>()
            .HasIndex(a => a.Number)
            .IsUnique();
    }
}