namespace HotChocolate.Types;

public struct MutationConventionOptions
{
    public string? InputTypeNamePattern { get; set; }

    public string? InputArgumentName { get; set; }

    public string? PayloadTypeNamePattern { get; set; }

    public string? PayloadErrorTypeNamePattern { get; set; }

    public string? PayloadErrorsFieldName { get; set; }

    public bool? ApplyToAllMutations { get; set; }
}
