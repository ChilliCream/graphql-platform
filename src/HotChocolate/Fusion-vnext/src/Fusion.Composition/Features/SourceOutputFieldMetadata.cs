using HotChocolate.Fusion.Info;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Features;

internal sealed class SourceOutputFieldMetadata
{
    public bool HasExternalDirective { get; set; }

    public bool HasInternalDirective { get; set; }

    public bool HasOverrideDirective { get; set; }

    public bool HasProvidesDirective { get; set; }

    public bool HasShareableDirective { get; set; }

    public bool IsExternal => HasExternalDirective;

    /// <summary>
    /// Gets a value indicating whether the field or its declaring type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; set; }

    /// <summary>
    /// Gets a value indicating whether the field or its declaring type is marked as internal.
    /// </summary>
    public bool IsInternal { get; set; }

    public bool IsKeyField { get; set; }

    public bool IsLookup { get; set; }

    public bool IsOverridden { get; set; }

    public bool IsShareable { get; set; }

    public ProvidesInfo? ProvidesInfo { get; set; }
}

internal sealed record ProvidesInfo(Directive Directive)
{
    public bool IsInvalidFieldsType { get; set; }

    public bool IsInvalidFieldsSyntax { get; set; }

    public SelectionSetNode? SelectionSet { get; set; }

    public List<FieldNodeInfo> FieldNodes { get; } = [];

    public List<MutableOutputFieldDefinition> Fields { get; } = [];
}
