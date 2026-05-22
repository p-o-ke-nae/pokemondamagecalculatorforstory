using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PokemonDamageCalculatorForStory.Extensions;

public static class ModelStateValidationExtensions
{
    public static void AddValidationFailures(this ModelStateDictionary modelState, IEnumerable<ValidationFailure> failures)
    {
        foreach (var failure in failures)
        {
            modelState.AddModelError(NormalizeKey(failure.PropertyName), failure.ErrorMessage);
        }
    }

    private static string NormalizeKey(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        var normalized = propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? propertyName;
        var collectionIndex = normalized.IndexOf('[', StringComparison.Ordinal);
        return collectionIndex >= 0 ? normalized[..collectionIndex] : normalized;
    }
}
