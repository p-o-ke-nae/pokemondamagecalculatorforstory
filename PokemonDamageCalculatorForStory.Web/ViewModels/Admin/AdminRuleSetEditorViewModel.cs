using PokemonDamageCalculatorForStory.Application.DTOs.Admin;

namespace PokemonDamageCalculatorForStory.ViewModels.Admin;

public sealed class AdminRuleSetEditorViewModel
{
    public Guid? Id { get; init; }
    public string Heading { get; init; } = string.Empty;
    public AdminRuleSetUpsertRequest Form { get; init; } = new("", 1, "", "", "Draft", "");
    public bool IsReferencedByRuns { get; init; }
    public IReadOnlyList<string> StatusOptions { get; init; } = [];
}
