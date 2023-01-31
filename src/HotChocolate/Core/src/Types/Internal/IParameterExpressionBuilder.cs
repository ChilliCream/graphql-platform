using System.Linq.Expressions;

#nullable enable

namespace HotChocolate.Internal;

/// <summary>
/// This interface represents an expression builder to resolver resolver parameter values.
/// </summary>
public interface IParameterExpressionBuilder : IParameterHandler
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
