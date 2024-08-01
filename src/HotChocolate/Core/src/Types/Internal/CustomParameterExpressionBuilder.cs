using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Internal;

/// <summary>
/// A custom parameter expression builder allows to implement custom resolver parameter
/// injection logic.
/// </summary>
public abstract class CustomParameterExpressionBuilder : IParameterExpressionBuilder
{
    ArgumentKind IParameterExpressionBuilder.Kind => ArgumentKind.Custom;

    bool IParameterExpressionBuilder.IsPure => false;

    bool IParameterExpressionBuilder.IsDefaultHandler => false;

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
    public abstract bool CanHandle(ParameterInfo parameter);

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
    public abstract Expression Build(ParameterExpressionBuilderContext context);
}

/// <summary>
/// A custom parameter expression builder that allows to specify the expressions by
/// passing them into the constructor.
/// </summary>
public class CustomParameterExpressionBuilder<TArg> : CustomParameterExpressionBuilder
{
    private readonly Func<ParameterInfo, bool> _canHandle;
    private readonly Expression<Func<IResolverContext, TArg>> _expression;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    public CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression)
    {
        _canHandle = p => p.ParameterType == typeof(TArg);
        _expression = expression;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="canHandle">
    /// A func that defines if a parameter can be handled by this expression builder.
    /// </param>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    public CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression,
        Func<ParameterInfo, bool> canHandle)
    {
        _expression = expression;
        _canHandle = canHandle;
    }

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
    public override bool CanHandle(ParameterInfo parameter)
        => _canHandle(parameter);

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
    public override Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Invoke(_expression, context.ResolverContext);
}
