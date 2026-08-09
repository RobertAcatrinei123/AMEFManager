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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(folder, "AMEFManager");
        
        Directory.CreateDirectory(path);
    
        var dbPath = Path.Combine(path, "storage.sqlite");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Amef>()
            .HasIndex(a => a.NUI)
            .IsUnique(); 

        modelBuilder.Entity<Amef>()
            .HasIndex(a => a.Series)
            .IsUnique(); 
        
        modelBuilder.Entity<Bill>()
            .HasIndex(a => new {a.BillSeries, a.BillNumber})
            .IsUnique();
        
        modelBuilder.Entity<Client>()
            .HasIndex(a => a.NationalIdentifier)
            .IsUnique();
        
        modelBuilder.Entity<Client>()
            .HasIndex(a => a.RegistrationNumber)
            .IsUnique();
        
        modelBuilder.Entity<Contract>()
            .HasIndex(a => a.Number)
            .IsUnique();
        
        modelBuilder.Entity<DeliveryDocument>()
            .HasIndex(a => a.Number)
            .IsUnique();
        
        modelBuilder.Entity<Person>()
            .HasIndex(a => a.Cnp)
            .IsUnique();    
        
        modelBuilder.Entity<Person>()
            .HasIndex(a => new {a.Series, a.Number})
            .IsUnique();
        
        modelBuilder.Entity<SealingDocument>()
            .HasIndex(a => a.Number)
            .IsUnique();
    }
}