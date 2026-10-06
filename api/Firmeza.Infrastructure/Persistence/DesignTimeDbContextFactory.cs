using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Firmeza.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FirmezaDbContext>
{
    public FirmezaDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<FirmezaDbContext>().UseNpgsql(
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Host=localhost;Database=firmeza;Username=firmeza").Options);
}
