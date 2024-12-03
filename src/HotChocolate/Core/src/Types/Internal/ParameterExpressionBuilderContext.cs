#nullable enable
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Internal;

/// <summary>
/// Represents the context to build the value injection expression for a resolver parameter.
/// </summary>
public readonly ref struct ParameterExpressionBuilderContext
{
    internal ParameterExpressionBuilderContext(
        ParameterInfo parameter,
        Expression resolverContext,
        IReadOnlyDictionary<ParameterInfo, string> argumentNameLookup)
    {
        Parameter = parameter;
        ResolverContext = resolverContext;

        if (argumentNameLookup.TryGetValue(parameter, out var name))
        {
            ArgumentName = name;
        }
    }

    /// <summary>
    /// Gets the parameter for which a value injection expression shall be built.
    /// </summary>
    public ParameterInfo Parameter { get; }

    /// <summary>
    /// Gets the expression to get access to the resolver context.
    /// </summary>
    public Expression ResolverContext { get; }

    /// <summary>
    /// If the current parameter represents a GraphQL argument and the argument's name
    /// differs from the c# parameter name, then this property will hold
    /// the GraphQL argument name.
    /// </summary>
    public string? ArgumentName { get; }
}
