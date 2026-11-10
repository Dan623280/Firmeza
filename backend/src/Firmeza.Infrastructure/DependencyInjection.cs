using Firmeza.Application.Interfaces;
using Firmeza.Application.Services;
using Firmeza.Infrastructure.External;
using Firmeza.Infrastructure.Persistence;
using Firmeza.Infrastructure.Persistence.Repositories;
using Firmeza.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
namespace Firmeza.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("Default")));
        services.AddIdentityCore<IdentityUser>(o => { o.Password.RequiredLength = 10; o.User.RequireUniqueEmail = true; o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
        services.AddAutoMapper(o => { o.AddProfile<MappingProfile>(); if (!string.IsNullOrWhiteSpace(config["Licenses:AutoMapper"])) o.LicenseKey = config["Licenses:AutoMapper"]; });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<SalesService>();
        if (!string.IsNullOrWhiteSpace(config["Licenses:EPPlus"]))
            ExcelPackage.License.SetCommercial(config["Licenses:EPPlus"]!);
        else
            ExcelPackage.License.SetNonCommercialPersonal(config["Licenses:StudentName"] ?? "Proyecto académico Firmeza");
        QuestPDF.Settings.License = LicenseType.Community;
        return services;
    }
}
