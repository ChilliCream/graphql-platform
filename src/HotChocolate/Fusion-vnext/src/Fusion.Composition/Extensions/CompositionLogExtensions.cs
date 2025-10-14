using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Logging.Contracts;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ValidationLogSeverity = HotChocolate.Logging.LogSeverity;

namespace HotChocolate.Fusion.Extensions;

internal static class CompositionLogExtensions
{
    public static void WriteValidationLog(
        this ICompositionLog log,
        IValidationLog validationLog,
        ISchemaDefinition schema)
    {
        foreach (var entry in validationLog)
        {
            log.Write(
                new LogEntry(
                    string.Format(
                        CompositionLogExtensions_EntryMessageWithSchemaName,
                        entry.Message,
                        schema.Name),
                    entry.Code,
                    MapLogSeverity(entry.Severity),
                    entry.Coordinate,
                    entry.TypeSystemMember,
                    schema,
                    entry.Extensions));
        }
    }

    private static LogSeverity MapLogSeverity(this ValidationLogSeverity severity)
    {
        return severity switch
        {
            ValidationLogSeverity.Info => LogSeverity.Info,
            ValidationLogSeverity.Warning => LogSeverity.Warning,
            ValidationLogSeverity.Error => LogSeverity.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }
}
