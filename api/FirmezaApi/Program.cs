using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Firmeza.Api.Configuration;
using Firmeza.Application.Authentication;
using Firmeza.Infrastructure;
using Firmeza.Infrastructure.Identity;
using Firmeza.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddFirmeza(builder.Configuration);
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(15)
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user) || user.SecurityStamp != context.Principal?.FindFirstValue("stamp"))
            {
                context.Fail("Account is unavailable.");
                return;
            }
            var roles = await userManager.GetRolesAsync(user);
            var tokenRoles = context.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
            if (!tokenRoles.SetEquals(roles))
            {
                context.Fail("Roles changed.");
                return;
            }
            if (roles.Contains(Roles.Customer))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<FirmezaDbContext>();
                if (!await db.Customers.AnyAsync(c => c.UserId == user.Id && c.IsActive, context.HttpContext.RequestAborted))
                    context.Fail("Customer profile is inactive.");
            }
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("authentication", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Firmeza Hardware Store API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() } });
});
var app = builder.Build();
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<FirmezaDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IdentityInitializer>().InitializeAsync();
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (FirmezaDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct) ? Results.Ok(new
        {
            status = "ready"
        }) : Results.StatusCode(503);
    }
    catch { return Results.StatusCode(503); }
});
app.Run();
public partial class Program
{
}
