namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Normalizes and validates agent addresses used throughout agent mail.
/// </summary>
internal static class MailAgentName
{
    /// <summary>
    /// Lowercases the given value and validates it starts with a letter or
    /// digit and contains only lowercase letters, digits, hyphens, and
    /// underscores. Throws <see cref="ExitException"/> when the value is
    /// empty, starts with any other character, or contains any other
    /// character; invalid characters are never stripped.
    /// </summary>
    public static string Normalize(string value)
    {
        var lowered = value.ToLowerInvariant();

        if (lowered.Length == 0)
        {
            throw new ExitException("An agent name must not be empty.");
        }

        // A leading hyphen or underscore is rejected so a mistyped option
        // reaching a recipient list is an error rather than a new agent.
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
