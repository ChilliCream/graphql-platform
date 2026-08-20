namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Normalizes and validates agent addresses used throughout agent mail.
/// </summary>
internal static class MailAgentName
{
    /// <summary>
    /// Lowercases the given value and validates it contains only lowercase
    /// letters, digits, hyphens, and underscores. Throws
    /// <see cref="ExitException"/> when the value is empty or contains any
    /// other character; invalid characters are never stripped.
    /// </summary>
    public static string Normalize(string value)
    {
        var lowered = value.ToLowerInvariant();

        if (lowered.Length == 0)
        {
            throw new ExitException("An agent name must not be empty.");
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
