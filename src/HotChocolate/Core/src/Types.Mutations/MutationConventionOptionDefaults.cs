namespace HotChocolate.Types;

internal static class MutationConventionOptionDefaults
{
    public const string MutationName = nameof(MutationName);

    public const string InputTypeNamePattern = $"{{{MutationName}}}Input";

    public const string InputArgumentName = "input";

    public const string PayloadTypeNamePattern = $"{{{MutationName}}}Payload";

    public const string ErrorTypeNamePattern = $"{{{MutationName}}}Error";

    public const string PayloadErrorsFieldName = "errors";

    public const bool ApplyToAllMutations = true;
}
