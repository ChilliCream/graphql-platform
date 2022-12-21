using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Represents the data to create a directive.
/// </summary>
public sealed class DirectiveDefinition
{
    /// <summary>
    /// Initializes a new instance of a <see cref="DirectiveDefinition"/>
    /// </summary>
    /// <param name="directiveNode">
    /// The directive syntax node.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="directiveNode"/> is <c>null</c>.
    /// </exception>
    public DirectiveDefinition(DirectiveNode directiveNode)
    {
        Value = directiveNode ?? throw new ArgumentNullException(nameof(directiveNode));
        Type = TypeReference.Create(directiveNode.Name.Value);
    }

    /// <summary>
    /// Initializes a new instance of a <see cref="DirectiveDefinition"/>
    /// </summary>
    /// <param name="directive">
    /// The runtime instance of a directive.
    /// </param>
    /// <param name="directiveType">
    /// The type reference to refer to the directive type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="directive"/> or <paramref name="directiveType"/> is <c>null</c>.
    /// </exception>
    public DirectiveDefinition(object directive, ITypeReference directiveType)
    {
        Value = directive ?? throw new ArgumentNullException(nameof(directive));
        Type = directiveType ?? throw new ArgumentNullException(nameof(directiveType));
    }

    public ITypeReference Type { get; }

    public object Value { get; }
}
