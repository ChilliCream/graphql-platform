using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Logging.Contracts;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ValidationLogSeverity = HotChocolate.Logging.LogSeverity;

namespace HotChocolate.Fusion.Extensions;

internal static class CompositionLogExtensions
{
    extension(ICompositionLog log)
    {
        public void WriteValidationLog(IValidationLog validationLog, ISchemaDefinition schema)
        {
            foreach (var entry in validationLog)
            {
                log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            CompositionLogExtensions_EntryMessageWithSchemaName,
                            entry.Message,
                            schema.Name)
                        .SetCode(entry.Code)
                        .SetSeverity(MapLogSeverity(entry.Severity))
                        .SetCoordinate(entry.Coordinate)
                        .SetTypeSystemMember(entry.TypeSystemMember)
                        .SetSchema(schema)
                        .SetExtensions(entry.Extensions)
                        .Build());
            }
        }

        private static LogSeverity MapLogSeverity(ValidationLogSeverity severity)
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
}
