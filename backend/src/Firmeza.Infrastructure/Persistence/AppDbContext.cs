using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Firmeza.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Product> Products => Set<Product>(); public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>(); public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>(); public DbSet<Rental> Rentals => Set<Rental>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Product>(e => { e.ToTable("Productos"); e.Property(p => p.Name).HasMaxLength(150); e.Property(p => p.Description).HasMaxLength(500); e.Property(p => p.Price).HasPrecision(12, 2); });
        b.Entity<Customer>(e => { e.ToTable("Clientes"); e.HasIndex(c => c.Document).IsUnique(); e.HasIndex(c => c.Email).IsUnique(); e.HasIndex(c => c.UserId).IsUnique(); e.Property(c => c.Document).HasMaxLength(30); e.Property(c => c.Name).HasMaxLength(100); e.Property(c => c.LastName).HasMaxLength(100); e.Property(c => c.Email).HasMaxLength(254); e.Property(c => c.Phone).HasMaxLength(30); e.Property(c => c.Address).HasMaxLength(250); e.HasOne<IdentityUser>().WithOne().HasForeignKey<Customer>(c => c.UserId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Sale>(e => { e.ToTable("Ventas"); e.HasIndex(s => s.Number).IsUnique(); e.Property(s => s.Status).HasConversion<string>(); e.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict); e.HasMany(s => s.Details).WithOne().HasForeignKey(d => d.SaleId); });
        b.Entity<SaleDetail>(e => { e.ToTable("DetalleVentas"); e.HasOne(d => d.Product).WithMany().HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Vehicle>(e => { e.ToTable("Vehiculos"); e.HasIndex(v => v.Plate).IsUnique(); });
        b.Entity<Rental>(e => { e.ToTable("Alquileres"); e.Property(r => r.Status).HasConversion<string>(); e.HasIndex(r => new { r.VehicleId, r.StartDate, r.EndDate }); e.HasOne(r => r.Vehicle).WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Restrict); e.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict); });
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(12);
                property.SetScale(2);
            }
    }
}
