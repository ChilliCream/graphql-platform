using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionDirective : IDirective
{
    public FusionDirective(
        FusionDirectiveDefinition definition,
        params ImmutableArray<ArgumentAssignment> arguments)
    {
        Definition = definition;
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    public string Name => Definition.Name;

    public FusionDirectiveDefinition Definition { get; }

    IDirectiveDefinition IDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="DirectiveNode"/> from an <see cref="FusionDirective"/>.
    /// </summary>
    public DirectiveNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    public T ToValue<T>() where T : notnull
        => throw new NotImplementedException();
}
