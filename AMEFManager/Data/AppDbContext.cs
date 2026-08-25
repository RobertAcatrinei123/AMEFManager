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
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(folder, "AMEFManager");
            
            Directory.CreateDirectory(path);
        
            var dbPath = Path.Combine(path, "storage.sqlite");
            optionsBuilder.UseSqlite($"Data Source={dbPath};Foreign Keys=True;");
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
        base.OnModelCreating(modelBuilder);

        // Unique Indexes
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

        // Foreign Key Delete Behaviors (DeleteBehavior.Restrict across all 12 relationships)

        // 1. Contract -> ContractType
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Type)
            .WithMany()
            .HasForeignKey(c => c.ContractTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Contract -> Client
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. Client -> Person
        modelBuilder.Entity<Client>()
            .HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Client -> Address
        modelBuilder.Entity<Client>()
            .HasOne(c => c.Address)
            .WithMany()
            .HasForeignKey(c => c.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // 5. Person -> Address
        modelBuilder.Entity<Person>()
            .HasOne(p => p.Address)
            .WithMany()
            .HasForeignKey(p => p.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // 6. Amef -> Authorization
        modelBuilder.Entity<Amef>()
            .HasOne(a => a.Authorization)
            .WithMany()
            .HasForeignKey(a => a.AuthorizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // 7. Amef -> Address
        modelBuilder.Entity<Amef>()
            .HasOne(a => a.Address)
            .WithMany()
            .HasForeignKey(a => a.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // 8. Amef -> Bill
        modelBuilder.Entity<Amef>()
            .HasOne(a => a.Bill)
            .WithMany()
            .HasForeignKey(a => a.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        // 9. Amef -> Contract
        modelBuilder.Entity<Amef>()
            .HasOne(a => a.Contract)
            .WithMany(c => c.Amefs)
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // 10. DeliveryDocument -> Amef
        modelBuilder.Entity<DeliveryDocument>()
            .HasOne(d => d.Amef)
            .WithMany()
            .HasForeignKey(d => d.AmefId)
            .OnDelete(DeleteBehavior.Restrict);

        // 11. SealingDocument -> Amef
        modelBuilder.Entity<SealingDocument>()
            .HasOne(s => s.Amef)
            .WithMany()
            .HasForeignKey(s => s.AmefId)
            .OnDelete(DeleteBehavior.Restrict);

        // 12. AdditionalDocument -> Contract
        modelBuilder.Entity<AdditionalDocument>()
            .HasOne(a => a.Contract)
            .WithMany(c => c.AdditionalDocuments)
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}