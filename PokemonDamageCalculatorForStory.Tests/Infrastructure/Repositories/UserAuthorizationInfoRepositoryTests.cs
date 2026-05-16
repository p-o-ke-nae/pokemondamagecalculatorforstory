using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;
using PokemonDamageCalculatorForStory.Tests.Infrastructure.TestSupport;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Infrastructure.Repositories;

public sealed class UserAuthorizationInfoRepositoryTests
{
    [Fact]
    public async Task FindByGoogleUserIdAsync_Returns_Permissions()
    {
        await using var database = await InfrastructureSqlServerTestDatabase.CreateAsync();
        await using var context = database.CreateDbContext();
        var repository = new UserAuthorizationInfoRepository(context);

        context.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = "admin-1",
                Role = "Administrator",
                Permissions = new List<PersistedUserPermission>
                {
                    new() { Permission = "weatherforecast.manage.any" },
                    new() { Permission = "weatherforecast.read.private" },
                },
            });
        await context.SaveChangesAsync();

        var loaded = await repository.FindByGoogleUserIdAsync("admin-1");

        Assert.NotNull(loaded);
        Assert.Equal("admin-1", loaded!.GoogleUserId);
        Assert.True(loaded.HasRole("Administrator"));
        Assert.True(loaded.HasPermission("weatherforecast.manage.any"));
        Assert.True(loaded.HasPermission("weatherforecast.read.private"));
    }
}