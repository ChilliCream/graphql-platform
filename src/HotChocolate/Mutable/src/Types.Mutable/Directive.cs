using System.Collections.Immutable;
using HotChocolate.Language;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

public sealed class Directive : IDirective
{
    public Directive(MutableDirectiveDefinition type, params ImmutableArray<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    public Directive(MutableDirectiveDefinition type, IEnumerable<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection([.. arguments]);
    }

    public string Name => Definition.Name;

    public MutableDirectiveDefinition Definition { get; }

    IDirectiveDefinition IDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="DirectiveNode"/> from an <see cref="Directive"/>.
    /// </summary>
    public DirectiveNode ToSyntaxNode()
        => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => Format(this);

    public T ToValue<T>() where T : notnull
        => throw new NotImplementedException();
}
