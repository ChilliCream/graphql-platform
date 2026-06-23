using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Metadata;

public sealed class SourceOutputField(
    string name,
    string schemaName,
    FieldRequirements? requirements,
    IType type,
    bool isExternal,
    SelectionSetNode? provides,
    string? sourceTypeName)
    : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public FieldRequirements? Requirements { get; } = requirements;

    public IType Type { get; } = type;

    public bool IsExternal { get; } = isExternal;

    public SelectionSetNode? Provides { get; } = provides;

    /// <summary>
    /// Gets the named type the source schema actually returns for this field when it differs
    /// from the composite field's named type, otherwise <c>null</c>.
    /// </summary>
    public string? SourceTypeName { get; } = sourceTypeName;
}
