using System.Text;

namespace Mocha.Transport.Nats;

/// <summary>
/// Derives NATS subjects, stream names and durable consumer names from Mocha endpoint names.
/// </summary>
/// <remarks>
/// Mocha's default conventions produce names such as <c>order-service.order-created</c> and
/// <c>order-created_error</c>. Dots are the natural subject separator in NATS so subjects use those
/// names verbatim, but dots are illegal in stream and consumer names and have to be replaced.
/// </remarks>
public static class NatsNaming
{
    /// <summary>
    /// The name length above which NATS storage directory names become unwieldy.
    /// </summary>
    public const int RecommendedMaxNameLength = 32;

    private static readonly char[] s_illegalNameCharacters = ['.', '*', '>', '/', '\\'];

    private const char ReplacementCharacter = '_';

    /// <summary>
    /// Derives the stream name for a service, upper-snake-cased by NATS convention.
    /// </summary>
    /// <param name="serviceName">The logical service name, for example <c>order-service</c>.</param>
    /// <returns>The stream name, for example <c>ORDER_SERVICE</c>.</returns>
    /// <example>order-service → ORDER_SERVICE</example>
    public static string ToStreamName(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        return Sanitize(serviceName, ReplacementCharacter).Replace('-', '_').ToUpperInvariant();
    }

    /// <summary>
    /// Derives the durable consumer name for a Mocha endpoint name.
    /// </summary>
    /// <param name="endpointName">The endpoint name, for example <c>order-service.order-created</c>.</param>
    /// <returns>The durable name, for example <c>order-service_order-created</c>.</returns>
    public static string ToDurableName(string endpointName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        return Sanitize(endpointName, ReplacementCharacter);
    }

    /// <summary>
    /// Determines whether a name is valid for a stream or a durable consumer.
    /// </summary>
    /// <param name="name">The name to test.</param>
    /// <returns><see langword="true"/> when the name contains no illegal characters.</returns>
    public static bool IsValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (IsIllegal(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a subject is well formed, allowing the <c>*</c> and <c>&gt;</c> wildcards.
    /// </summary>
    /// <param name="subject">The subject to test.</param>
    /// <returns><see langword="true"/> when every token is non-empty and free of whitespace.</returns>
    public static bool IsValidSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }

        var tokens = subject.Split('.');

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            if (token.Length == 0)
            {
                return false;
            }

            if (token == ">" && i != tokens.Length - 1)
            {
                return false;
            }

            foreach (var character in token)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string Sanitize(string value, char replacement)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(IsIllegal(character) ? replacement : character);
        }

        return builder.ToString();
    }

    private static bool IsIllegal(char character)
    {
        if (char.IsWhiteSpace(character) || char.IsControl(character))
        {
            return true;
        }

        foreach (var illegal in s_illegalNameCharacters)
        {
            if (character == illegal)
            {
                return true;
            }
        }

        return false;
    }
}
