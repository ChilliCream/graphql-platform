namespace HotChocolate.Types;

public struct MutationFieldOptions
{
    public string? InputTypeName { get; set; }

    public string? InputArgumentName { get; set; }

    public string? PayloadTypeName { get; set; }

    public string? PayloadFieldName { get; set; }

    public string? PayloadErrorTypeName { get; set; }

    public string? PayloadErrorsFieldName { get; set; }

    public bool Disable { get; set; } = false;
}
