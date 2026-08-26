using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Port=5432;Database=municipal_platform;Username=municipal";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection)
            .Options;
        var tenant = new TenantContext();
        tenant.SetMunicipality(Guid.Parse("00000000-0000-0000-0000-000000000001"), "design-time");
        return new ApplicationDbContext(options, tenant);
    }
}
