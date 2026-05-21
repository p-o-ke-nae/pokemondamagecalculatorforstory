using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.Entities;

public sealed class UserAuthorizationInfoTests
{
    [Fact]
    public void Create_Rejects_Duplicate_Permissions()
    {
        Assert.Throws<ValidationException>(() => UserAuthorizationInfo.Create("user-1", "Administrator", ["masters.view", "masters.view"]));
    }

    [Fact]
    public void Update_Replaces_Role_And_Permissions()
    {
        var entity = UserAuthorizationInfo.Create("user-1", "Member", []);

        entity.Update("MasterEditor", ["masters.view", "masters.rulesets.manage"]);

        Assert.Equal("MasterEditor", entity.Role);
        Assert.Contains("masters.rulesets.manage", entity.Permissions);
    }
}
