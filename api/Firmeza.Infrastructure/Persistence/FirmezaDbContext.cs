using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class FirmezaDbContext(DbContextOptions<FirmezaDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Rental> Rentals => Set<Rental>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ImportBatch>(e => { e.HasKey(x => x.Hash); e.Property(x => x.Hash).HasMaxLength(64); });
        b.Entity<Product>(e =>
        {
            e.ToTable("Products", t => { t.HasCheckConstraint("CK_Product_Stock", "\"Stock\" >= 0"); t.HasCheckConstraint("CK_Product_Price", "\"Price\" > 0"); t.HasCheckConstraint("CK_Product_TaxRate", "\"TaxRate\" BETWEEN 0 AND 1"); });
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.TaxRate).HasPrecision(5, 4);
        });
        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.DocumentNumber).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.DocumentNumber).HasMaxLength(30);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(25);
            e.Property(x => x.Address).HasMaxLength(300);
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Customer>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Vehicle>(e =>
        {
            e.ToTable("Vehicles", t => t.HasCheckConstraint("CK_Vehicle_Price", "\"DailyPrice\" > 0"));
            e.HasIndex(x => x.LicensePlate).IsUnique();
            e.Property(x => x.LicensePlate).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Type).HasMaxLength(100);
            e.Property(x => x.Brand).HasMaxLength(100);
        });
        b.Entity<Sale>(e =>
        {
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Number).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        b.Entity<SaleItem>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(150);
            e.Property(x => x.TaxRate).HasPrecision(5, 4);
            e.ToTable("SaleItems", t => t.HasCheckConstraint("CK_SaleItem_Quantity", "\"Quantity\" > 0"));
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Rental>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.VehicleId, x.StartDate, x.EndDate });
            e.ToTable("Rentals", t => t.HasCheckConstraint("CK_Rental_Period", "\"EndDate\" > \"StartDate\""));
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<IdempotencyRecord>(e =>
        {
            e.HasIndex(x => new { x.CustomerId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Hash).HasMaxLength(64);
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OutboxMessage>(e =>
        {
            e.HasIndex(x => x.SaleId).IsUnique();
            e.HasIndex(x => new { x.Status, x.NextAttemptAt });
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.ReceiptKey).HasMaxLength(100);
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        });
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                if (property.ClrType == typeof(decimal) && property.GetPrecision() is null)
                {
                    property.SetPrecision(12);
                    property.SetScale(2);
                }
    }
}
