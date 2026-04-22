using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Metadata;

public sealed class SourceOutputField(
    string name,
    string schemaName,
    FieldRequirements? requirements,
    IType type,
    bool isExternal,
    SelectionSetNode? provides)
    : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public FieldRequirements? Requirements { get; } = requirements;

    public IType Type { get; } = type;

    public bool IsExternal { get; } = isExternal;

    public SelectionSetNode? Provides { get; } = provides;

    public int BaseCost => 1;
}
