namespace PokemonDamageCalculatorForStory.Authentication;

public class GoogleAccessTokenValidationResult
{
    private GoogleAccessTokenValidationResult(bool isValid, string? googleUserId, string? email, string? name, string? failureReason)
    {
        IsValid = isValid;
        GoogleUserId = googleUserId;
        Email = email;
        Name = name;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }
    public string? GoogleUserId { get; }
    public string? Email { get; }
    public string? Name { get; }
    public string? FailureReason { get; }

    public static GoogleAccessTokenValidationResult Valid(string googleUserId, string email, string name)
    {
        return new GoogleAccessTokenValidationResult(true, googleUserId, email, name, null);
    }

    public static GoogleAccessTokenValidationResult Invalid(string failureReason)
    {
        return new GoogleAccessTokenValidationResult(false, null, null, null, failureReason);
    }
}