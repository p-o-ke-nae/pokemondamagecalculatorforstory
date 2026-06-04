using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Infrastructure.Repositories;

public sealed class UserAuthorizationInfoRepositoryTests
{
    [Fact]
    public async Task FindByGoogleUserIdAsync_Returns_Permissions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        var repository = new UserAuthorizationInfoRepository(context);

        context.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = "admin-1",
                Role = "Administrator",
                Permissions = new List<PersistedUserPermission>
                {
                    new() { Permission = "runs.manage.any" },
                },
            });
        await context.SaveChangesAsync();

        var loaded = await repository.FindByGoogleUserIdAsync("admin-1");

        Assert.NotNull(loaded);
        Assert.Equal("admin-1", loaded!.GoogleUserId);
        Assert.True(loaded.HasRole("Administrator"));
        Assert.True(loaded.HasPermission("runs.manage.any"));
    }
}
