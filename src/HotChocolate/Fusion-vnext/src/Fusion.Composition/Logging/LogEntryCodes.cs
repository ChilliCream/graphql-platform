namespace HotChocolate.Fusion.Logging;

public static class LogEntryCodes
{
    public const string DisallowedInaccessible = "DISALLOWED_INACCESSIBLE";
    public const string ExternalArgumentDefaultMismatch = "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH";
    public const string ExternalMissingOnBase = "EXTERNAL_MISSING_ON_BASE";
    public const string ExternalUnused = "EXTERNAL_UNUSED";
    public const string OutputFieldTypesNotMergeable = "OUTPUT_FIELD_TYPES_NOT_MERGEABLE";
    public const string RootMutationUsed = "ROOT_MUTATION_USED";
    public const string RootQueryUsed = "ROOT_QUERY_USED";
}
