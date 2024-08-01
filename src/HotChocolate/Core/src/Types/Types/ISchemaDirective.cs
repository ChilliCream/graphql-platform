using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

/// <summary>
/// Represents a schema building directive that allows to rewrite
/// the schema when using SDL first.
/// </summary>
public interface ISchemaDirective
{
    /// <summary>
    /// Gets the name of the schema directive.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies the configuration of this directive
    /// to the annotated type system member.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="directiveNode">
    /// The directive node.
    /// </param>
    /// <param name="definition">
    /// The type system member definition.
    /// </param>
    /// <param name="path">
    /// The parent type definitions.
    /// </param>
    void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        IDefinition definition,
        Stack<IDefinition> path);
}
