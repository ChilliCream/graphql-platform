using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Logging.Contracts;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ValidationLogSeverity = HotChocolate.Logging.LogSeverity;
using ValidationLogEntryCodes = HotChocolate.Logging.LogEntryCodes;

namespace HotChocolate.Fusion.Extensions;

internal static class CompositionLogExtensions
{
    extension(ICompositionLog log)
    {
        public void WriteValidationLog(
            IValidationLog validationLog,
            ISchemaDefinition schema,
            LogSeverity invalidFieldDeprecationSeverity)
        {
            foreach (var entry in validationLog)
            {
                // The deprecation-consistency finding (an implementing field deprecated while the
                // interface field is not) is surfaced at a configurable severity so composition can
                // treat it as a warning; every other finding keeps its validator severity.
                var severity =
                    entry.Code == ValidationLogEntryCodes.InvalidFieldDeprecation
                        ? invalidFieldDeprecationSeverity
                        : MapLogSeverity(entry.Severity);

                log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            CompositionLogExtensions_EntryMessageWithSchemaName,
                            entry.Message,
                            schema.Name)
                        .SetCode(entry.Code)
                        .SetSeverity(severity)
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
