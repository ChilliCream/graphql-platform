using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// The exception that is thrown when schema composition fails.
/// </summary>
public sealed class SchemaCompositionException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaCompositionException"/>.
    /// </summary>
    /// <param name="compositionLog">The composition log containing the errors.</param>
    public SchemaCompositionException(CompositionLog compositionLog)
        : base(BuildMessage(compositionLog))
    {
        CompositionLog = compositionLog;
    }

    /// <summary>
    /// Gets the composition log containing the detailed errors and warnings
    /// encountered during schema composition.
    /// </summary>
    public CompositionLog CompositionLog { get; }

    private static string BuildMessage(CompositionLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var messages = new List<string>();

        foreach (var entry in log)
        {
            if (entry.Severity == LogSeverity.Error)
            {
                messages.Add(entry.Message);
            }
        }

        return messages.Count switch
        {
            0 => "Schema composition failed.",
            1 => $"Schema composition failed: {messages[0]}",
            _ => $"Schema composition failed with {messages.Count} errors: {messages[0]}"
        };
    }
}
