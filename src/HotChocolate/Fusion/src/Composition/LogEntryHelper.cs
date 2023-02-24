using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;

namespace HotChocolate.Fusion.Composition;

internal static class LogEntryHelper
{
    public static LogEntry RemoveMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RemoveMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            coordinate,
            schema);
}

public static class LogEntryCodes
{
    public const string RemoveMemberNotFound = "HF0001";
}
