using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using Xunit;

namespace PokemonDamageCalculatorForStory.Tests.Domain.Entities;

public sealed class RuleSetTests
{
    [Fact]
    public void Create_Rejects_Unknown_Status()
    {
        Assert.Throws<ValidationException>(() => RuleSet.Create("gen6-standard", 6, "title", "1.0", "Unknown", ""));
    }

    [Fact]
    public void Update_Changes_Values()
    {
        var ruleSet = RuleSet.Create("gen6-standard", 6, "title", "1.0", RuleSetStatuses.Draft, "");

        ruleSet.Update("gen6-standard", 6, "updated", "1.1", RuleSetStatuses.Active, "summary");

        Assert.Equal("updated", ruleSet.Title);
        Assert.Equal(RuleSetStatuses.Active, ruleSet.Status);
    }
}
