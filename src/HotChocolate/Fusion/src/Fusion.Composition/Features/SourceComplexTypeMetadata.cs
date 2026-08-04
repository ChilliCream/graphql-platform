using HotChocolate.Fusion.Info;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Features;

internal sealed class SourceComplexTypeMetadata
{
    public Dictionary<Directive, KeyInfo> KeyInfoByDirective { get; } = [];
}

internal sealed record KeyInfo
{
    public bool IsInvalidFieldsType { get; set; }

    public bool IsInvalidFieldsSyntax { get; set; }

    public List<FieldNodeInfo> FieldNodes { get; } = [];

    public List<MutableOutputFieldDefinition> Fields { get; } = [];
}
