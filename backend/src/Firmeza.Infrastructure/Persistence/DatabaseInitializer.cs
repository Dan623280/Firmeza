using Firmeza.Domain.Constants;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Firmeza.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider provider, IConfiguration config)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Administrator, Roles.Customer })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && await users.FindByEmailAsync(email) == null)
        {
            var user = new IdentityUser { UserName = email, Email = email };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, Roles.Administrator);
        }
        if (config.GetValue("Seed:DemoData", false) && !await db.Products.AnyAsync())
        {
            db.Products.AddRange(new Product { Name = "Cemento estructural 50 kg", Description = "Resistencia y calidad para tus proyectos de obra gris.", Price = 34500, Stock = 150 }, new Product { Name = "Arena de río · m³", Description = "Agregado fino seleccionado para mezclas y acabados.", Price = 89000, Stock = 60 }, new Product { Name = "Ladrillo de arcilla", Description = "Unidad de mampostería para construir con confianza.", Price = 1200, Stock = 3000 }, new Product { Name = "Varilla corrugada media pulgada", Description = "Acero de refuerzo para estructuras de concreto.", Price = 28500, Stock = 220 }, new Product { Name = "Grava triturada · m³", Description = "Agregado grueso para concreto y cimentación.", Price = 95000, Stock = 75 }, new Product { Name = "Bloque de concreto", Description = "Solución práctica para muros y cerramientos.", Price = 3800, Stock = 650 });
            db.Vehicles.AddRange(new Vehicle { Name = "Volqueta de obra", Type = "Volqueta", Brand = "Chevrolet", Plate = "FRM101", PricePerDay = 350000 }, new Vehicle { Name = "Camión de carga", Type = "Camión", Brand = "Hino", Plate = "FRM202", PricePerDay = 280000 });
            await db.SaveChangesAsync();
        }
    }
}
