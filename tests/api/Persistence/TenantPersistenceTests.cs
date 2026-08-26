using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Persistence;

public sealed class TenantPersistenceTests
{
    [Fact]
    public async Task QueryFilterHidesRowsFromAnotherMunicipality()
    {
        var databaseName = $"tenant-filter-{Guid.NewGuid():N}";
        var firstMunicipality = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondMunicipality = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var firstContext = CreateContext(options, firstMunicipality, "deodapolis"))
        {
            firstContext.NewsArticles.Add(NewsArticle.Create(firstMunicipality, "Primeira", "primeira", Guid.NewGuid()));
            await firstContext.SaveChangesAsync();
        }

        await using (var secondContext = CreateContext(options, secondMunicipality, "outra"))
        {
            secondContext.NewsArticles.Add(NewsArticle.Create(secondMunicipality, "Segunda", "segunda", Guid.NewGuid()));
            await secondContext.SaveChangesAsync();
        }

        await using var queryContext = CreateContext(options, firstMunicipality, "deodapolis");
        var titles = await queryContext.NewsArticles.Select(article => article.Title).ToListAsync();

        Assert.Equal(["Primeira"], titles);
    }

    [Fact]
    public async Task SaveChangesRejectsEntityFromAnotherMunicipality()
    {
        var firstMunicipality = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondMunicipality = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tenant-write-{Guid.NewGuid():N}")
            .Options;
        await using var context = CreateContext(options, firstMunicipality, "deodapolis");
        context.NewsArticles.Add(NewsArticle.Create(secondMunicipality, "Inválida", "invalida", Guid.NewGuid()));

        var error = await Assert.ThrowsAsync<TenantPersistenceException>(
            () => context.SaveChangesAsync());

        Assert.Contains("outro município", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateContext(
        DbContextOptions<ApplicationDbContext> options,
        Guid municipalityId,
        string slug)
    {
        var tenant = new TenantContext();
        tenant.SetMunicipality(municipalityId, slug);
        return new ApplicationDbContext(options, tenant);
    }
}
