namespace BallastLane.Application.Accounts;

public static class AccountRules
{
    public const int DisplayNameMaxLength = 80;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const string PasswordMessage = "Password must be 8 to 128 characters and include a letter and a number.";

    public static Dictionary<string, string[]> ValidateRegistration(string? email, string? password, string? displayName)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsValidEmail(email))
        {
            errors["email"] = ["Enter a valid email address."];
        }

        AddPasswordErrors(errors, password);
        AddDisplayNameErrors(errors, displayName);
        return errors;
    }

    public static Dictionary<string, string[]> ValidateLogin(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors["password"] = ["Password is required."];
        }

        return errors;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var value = email.Trim();
        if (value.Length > 256 || value.Contains(' '))
        {
            return false;
        }

        var at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@'))
        {
            return false;
        }

        var host = value[(at + 1)..];
        return host.Contains('.') && !host.StartsWith('.') && !host.EndsWith('.') && !host.Contains("..");
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizeDisplayName(string displayName) => displayName.Trim();

    private static void AddPasswordErrors(Dictionary<string, string[]> errors, string? password)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length < PasswordMinLength
            || password.Length > PasswordMaxLength
            || !password.Any(char.IsLetter)
            || !password.Any(char.IsDigit))
        {
            errors["password"] = [PasswordMessage];
        }
    }

    private static void AddDisplayNameErrors(Dictionary<string, string[]> errors, string? displayName)
    {
        var name = displayName?.Trim() ?? string.Empty;
        if (name.Length < 2)
        {
            errors["displayName"] = ["Display name must be at least 2 characters."];
        }
        else if (name.Length > DisplayNameMaxLength)
        {
            errors["displayName"] = [$"Display name must be {DisplayNameMaxLength} characters or fewer."];
        }
    }
}
