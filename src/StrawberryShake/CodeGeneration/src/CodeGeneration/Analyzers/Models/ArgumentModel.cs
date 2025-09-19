using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents a model for an operation argument (GraphQL variable declaration).
/// </summary>
public sealed class ArgumentModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentModel" />
    /// </summary>
    /// <param name="name">The name of the argument.</param>
    /// <param name="type">The GraphQL schema type of the argument.</param>
    /// <param name="variable">The variable declaration from the query syntax tree.</param>
    /// <param name="defaultValue">The default value.</param>
    public ArgumentModel(
        string name,
        IInputType type,
        VariableDefinitionNode variable,
        IValueNode? defaultValue)
    {
        Name = name.EnsureGraphQLName();
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Variable = variable ?? throw new ArgumentNullException(nameof(variable));
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets the name of the argument.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the GraphQL schema type of the argument.
    /// </summary>
    public IInputType Type { get; }

    /// <summary>
    /// Gets the variable declaration from the query syntax tree.
    /// </summary>
    public VariableDefinitionNode Variable { get; }

    /// <summary>
    /// Gets the default value.
    /// </summary>
    public IValueNode? DefaultValue { get; }
}
