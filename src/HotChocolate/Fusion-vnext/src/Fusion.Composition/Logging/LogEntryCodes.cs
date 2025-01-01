namespace HotChocolate.Fusion.Logging;

public static class LogEntryCodes
{
    public const string DisallowedInaccessible = "DISALLOWED_INACCESSIBLE";
    public const string ExternalArgumentDefaultMismatch = "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH";
    public const string ExternalMissingOnBase = "EXTERNAL_MISSING_ON_BASE";
    public const string ExternalOnInterface = "EXTERNAL_ON_INTERFACE";
    public const string ExternalUnused = "EXTERNAL_UNUSED";
    public const string KeyDirectiveInFieldsArg = "KEY_DIRECTIVE_IN_FIELDS_ARG";
    public const string KeyFieldsHasArgs = "KEY_FIELDS_HAS_ARGS";
    public const string KeyFieldsSelectInvalidType = "KEY_FIELDS_SELECT_INVALID_TYPE";
    public const string KeyInvalidFields = "KEY_INVALID_FIELDS";
    public const string KeyInvalidSyntax = "KEY_INVALID_SYNTAX";
    public const string OutputFieldTypesNotMergeable = "OUTPUT_FIELD_TYPES_NOT_MERGEABLE";
    public const string ProvidesDirectiveInFieldsArg = "PROVIDES_DIRECTIVE_IN_FIELDS_ARG";
    public const string ProvidesFieldsHasArgs = "PROVIDES_FIELDS_HAS_ARGS";
    public const string ProvidesFieldsMissingExternal = "PROVIDES_FIELDS_MISSING_EXTERNAL";
    public const string ProvidesOnNonCompositeField = "PROVIDES_ON_NON_COMPOSITE_FIELD";
    public const string RootMutationUsed = "ROOT_MUTATION_USED";
    public const string RootQueryUsed = "ROOT_QUERY_USED";
    public const string RootSubscriptionUsed = "ROOT_SUBSCRIPTION_USED";
}
