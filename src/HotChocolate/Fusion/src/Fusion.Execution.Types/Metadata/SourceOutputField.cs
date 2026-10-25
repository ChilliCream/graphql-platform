using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Metadata;

public sealed class SourceOutputField : ISourceMember
{
    public SourceOutputField(
        string name,
        string schemaName,
        FieldRequirements? requirements,
        IType type,
        bool isExternal,
        bool isSourceExternal,
        SelectionSetNode? provides,
        string? sourceTypeName,
        EventStreamDirective? eventStreamDirective = null)
    {
        Name = name;
        SchemaName = schemaName;
        Requirements = requirements;
        Type = type;
        IsExternal = isExternal;
        IsSourceExternal = isSourceExternal;
        Provides = provides;
        SourceTypeName = sourceTypeName;
        EventStreamDirective = eventStreamDirective;
    }

    public SourceOutputField(
        string name,
        string schemaName,
        FieldRequirements? requirements,
        IType type,
        bool isExternal,
        SelectionSetNode? provides,
        string? sourceTypeName,
        EventStreamDirective? eventStreamDirective = null)
        : this(
            name,
            schemaName,
            requirements,
            type,
            isExternal,
            false,
            provides,
            sourceTypeName,
            eventStreamDirective)
    {
    }

    public string Name { get; }

    public string SchemaName { get; }

    public FieldRequirements? Requirements { get; }

    public IType Type { get; }

    public bool IsExternal { get; }

    /// <summary>
    /// Gets whether connector preprocessing promoted a source field that was originally external.
    /// </summary>
    public bool IsSourceExternal { get; }

    public SelectionSetNode? Provides { get; }

    /// <summary>
    /// Gets the named type the source schema actually returns for this field when it differs
    /// from the composite field's named type, otherwise <c>null</c>.
    /// </summary>
    public string? SourceTypeName { get; }

    public EventStreamDirective? EventStreamDirective { get; }
}
