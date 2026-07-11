using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Resolvers;

namespace HotChocolate.Internal;

/// <summary>
/// A custom parameter expression builder allows to implement custom resolver parameter
/// injection logic.
/// </summary>
public abstract class CustomParameterExpressionBuilder : IParameterExpressionBuilder
{
    private readonly bool _isPure;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>
    /// that is not considered pure.
    /// </summary>
    protected CustomParameterExpressionBuilder() { }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>
    /// with an explicit purity setting.
    /// </summary>
    /// <param name="isPure">
    /// Defines if the parameter expression can be used for pure resolvers.
    /// </param>
    internal CustomParameterExpressionBuilder(bool isPure)
    {
        _isPure = isPure;
    }

    ArgumentKind IParameterExpressionBuilder.Kind => ArgumentKind.Custom;

    bool IParameterExpressionBuilder.IsPure => _isPure;

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

    internal virtual bool RequiresParameterInfo(ParameterDescriptor parameter)
        => false;
}

/// <summary>
/// A custom parameter expression builder that allows to specify the expressions by
/// passing them into the constructor.
/// </summary>
public class CustomParameterExpressionBuilder<TArg>
    : CustomParameterExpressionBuilder
    , IParameterBindingFactory
{
    private readonly Func<ParameterInfo, bool> _canHandle;
    private readonly Expression<Func<IResolverContext, TArg>> _expression;
    private readonly bool _matchesParameterType;
    private readonly Lazy<Func<IResolverContext, TArg>> _compiledExpression;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    public CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression)
        : this(expression, static p => p.ParameterType == typeof(TArg), isPure: false, matchesParameterType: true)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    /// <param name="isPure">
    /// Defines if the parameter expression can be used for pure resolvers.
    /// </param>
    internal CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression,
        bool isPure)
        : this(expression, static p => p.ParameterType == typeof(TArg), isPure, matchesParameterType: true)
    { }

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
        : this(expression, canHandle, isPure: false, matchesParameterType: false)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="canHandle">
    /// A func that defines if a parameter can be handled by this expression builder.
    /// </param>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    /// <param name="isPure">
    /// Defines if the parameter expression can be used for pure resolvers.
    /// </param>
    internal CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression,
        Func<ParameterInfo, bool> canHandle,
        bool isPure)
        : this(expression, canHandle, isPure, matchesParameterType: false)
    { }

    private CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression,
        Func<ParameterInfo, bool> canHandle,
        bool isPure,
        bool matchesParameterType)
        : base(isPure)
    {
        _expression = expression;
        _canHandle = canHandle;
        _matchesParameterType = matchesParameterType;
        _compiledExpression = new(CompileExpression);
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

    ArgumentKind IParameterBindingFactory.Kind => ArgumentKind.Custom;

    bool IParameterBindingFactory.IsPure => ((IParameterExpressionBuilder)this).IsPure;

    bool IParameterBindingFactory.IsDefaultHandler => false;

    bool IParameterBindingFactory.CanHandle(ParameterDescriptor parameter)
        => _matchesParameterType && parameter.Type == typeof(TArg);

    IParameterBinding IParameterBindingFactory.Create(ParameterDescriptor parameter)
        => new CustomParameterBinding(
            _compiledExpression.Value,
            ((IParameterExpressionBuilder)this).IsPure);

    internal override bool RequiresParameterInfo(ParameterDescriptor parameter)
        => !_matchesParameterType && parameter.Type.IsAssignableFrom(typeof(TArg));

    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification =
            "The expression interpreter is used when dynamic code is unavailable.")]
    private Func<IResolverContext, TArg> CompileExpression()
        => _expression.Compile(preferInterpretation: !RuntimeFeature.IsDynamicCodeSupported);

    private sealed class CustomParameterBinding(
        Func<IResolverContext, TArg> compiledExpression,
        bool isPure)
        : IParameterBinding
    {
        public bool IsPure { get; } = isPure;

        public T Execute<T>(IResolverContext context)
        {
            Debug.Assert(typeof(T) == typeof(TArg));
            var value = compiledExpression(context);
            return Unsafe.As<TArg, T>(ref value);
        }
    }
}
