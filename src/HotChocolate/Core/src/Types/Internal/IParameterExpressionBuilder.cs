using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Internal;

/// <summary>
/// This interface represents an expression builder to resolver parameter values.
/// </summary>
public interface IParameterExpressionBuilder
{
    /// <summary>
    /// Defines the argument kind that is handled by this builder.
    /// </summary>
    ArgumentKind Kind { get; }

    /// <summary>
    /// Specifies if this expression builder can build parameter value resolvers
    /// for pure resolvers.
    /// </summary>
    bool IsPure { get; }

    /// <summary>
    /// Specifies that this handler is run after all non-default handlers.
    /// </summary>
    bool IsDefaultHandler { get; }

    /// <summary>
    /// Checks if this expression builder can handle the following parameter.
    /// </summary>
    /// <param name="parameter">
    /// The parameter that needs to be resolved.
    /// </param>
    /// <returns>
    /// <c>true</c> if the parameter can be handled by this expression builder;
    /// otherwise <c>false</c>.
    /// </returns>
    bool CanHandle(ParameterInfo parameter);

    /// <summary>
    /// Builds an expression that resolves a resolver parameter.
    /// </summary>
    /// <param name="context">
    /// The parameter expression builder context.
    /// </param>
    /// <returns>
    /// Returns an expression the handles the value injection into the parameter specified by
    /// <see cref="ParameterExpressionBuilderContext.Parameter"/>.
    /// </returns>
    Expression Build(ParameterExpressionBuilderContext context);
}
