namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Normalizes and validates agent addresses used throughout agent mail.
/// </summary>
internal static class MailAgentName
{
    /// <summary>
    /// Lowercases the address and requires an ASCII letter or digit first, followed by
    /// ASCII letters, digits, hyphens, or underscores. Throws <see cref="ExitException"/>
    /// for empty or invalid addresses without stripping characters.
    /// </summary>
    public static string Normalize(string value)
    {
        var lowered = value.ToLowerInvariant();

        if (lowered.Length == 0)
        {
            throw new ExitException("An agent name must not be empty.");
        }

        if (lowered[0] is not (>= 'a' and <= 'z' or >= '0' and <= '9'))
        {
            throw new ExitException(
                $"Invalid agent name '{value}'. Agent names must start with a lowercase "
                + "letter or a digit.");
        }

        foreach (var c in lowered)
        {
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
            {
                throw new ExitException(
                    $"Invalid agent name '{value}'. Agent names may only contain "
                    + "lowercase letters, digits, hyphens, and underscores.");
            }
        }

        return lowered;
    }
}
