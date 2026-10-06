using Firmeza.Application.Abstractions;
using Firmeza.Application.Authentication;
using Firmeza.Application.Products;
using Firmeza.Application.Customers;
using Firmeza.Application.Vehicles;
using Firmeza.Application.Sales;
using Firmeza.Application.Rentals;
using Firmeza.Application.Reporting;
using Firmeza.Application.Common;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Firmeza.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFirmeza(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection.");
        services.AddDbContext<FirmezaDbContext>(o => o.UseNpgsql(connection));
        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequiredLength = 12;
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FirmezaDbContext>();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt")).Validate(o => System.Text.Encoding.UTF8.GetByteCount(o.SigningKey) >= 32 && o.ExpirationMinutes is >= 1 and <= 120 && !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience), "Configure a JWT key of at least 32 bytes, issuer, audience and a 1–120 minute lifetime.").ValidateOnStart();
        services.AddAutoMapper(o => { o.LicenseKey = configuration["AutoMapper:LicenseKey"]; }, typeof(MappingProfile));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<UnitOfWork>());
        services.AddScoped<IBusinessTransaction>(s => s.GetRequiredService<UnitOfWork>());
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IDeliveryQueue, DeliveryQueue>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<SaleService>();
        services.AddScoped<RentalService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentCustomer, CurrentCustomer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IdentityInitializer>();
        services.AddScoped<IReceiptGenerator, Firmeza.Infrastructure.Documents.PdfReceiptGenerator>();
        services.AddScoped<IReceiptStorage, Firmeza.Infrastructure.Documents.FileReceiptStorage>();
        services.AddScoped<IReceiptReader, Firmeza.Infrastructure.Documents.ReceiptReader>();
        services.AddScoped<IEmailSender, Firmeza.Infrastructure.Email.SmtpEmailSender>();
        services.AddScoped<Firmeza.Infrastructure.Email.DeliveryProcessor>();
        if (configuration.GetValue("Delivery:Enabled", false))
            services.AddHostedService<Firmeza.Infrastructure.Email.DeliveryWorker>();
        services.AddScoped<Firmeza.Infrastructure.DataExchange.ExcelReader>();
        services.AddScoped<Firmeza.Infrastructure.DataExchange.ExcelImporter>();
        services.AddScoped<Firmeza.Infrastructure.DataExchange.DataExporter>();
        services.AddScoped<Firmeza.Application.DataExchange.IDataExchangeService, Firmeza.Infrastructure.DataExchange.DataExchangeService>();
        return services;
    }
}
