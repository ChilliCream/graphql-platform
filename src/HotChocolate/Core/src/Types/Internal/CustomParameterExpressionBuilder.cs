using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Internal;

/// <summary>
/// A custom parameter expression builder allows to implement custom resolver parameter
/// injection logic.
/// </summary>
public abstract class CustomParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
{
    private protected readonly bool _isPure;

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

    ArgumentKind IParameterBindingFactory.Kind => ArgumentKind.Custom;

    bool IParameterBindingFactory.IsPure => _isPure;

    bool IParameterBindingFactory.IsDefaultHandler => false;

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
    /// Checks if this expression builder can handle the parameter described by the
    /// source generator (which has no <see cref="ParameterInfo"/>).
    /// </summary>
    public virtual bool CanHandle(ParameterDescriptor parameter) => false;

    /// <summary>
    /// Creates the runtime binding used by source-generated resolvers.
    /// </summary>
    public virtual IParameterBinding Create(ParameterDescriptor parameter)
        => throw new NotSupportedException();

    /// <summary>
    /// Returns <c>true</c> when this builder targets the described parameter but can only
    /// decide via a <see cref="ParameterInfo"/> predicate, which the source generator cannot
    /// evaluate. Used to raise a clear error instead of silently misclassifying the parameter.
    /// </summary>
    internal virtual bool RequiresParameterInfo(ParameterDescriptor parameter) => false;

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
    private readonly bool _usesDefaultTypePredicate;
    private Func<IResolverContext, TArg>? _compiled;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomParameterExpressionBuilder"/>.
    /// </summary>
    /// <param name="expression">
    /// The expression that shall be used to resolve the parameter value.
    /// </param>
    public CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression)
        : base(isPure: false)
    {
        _canHandle = p => p.ParameterType == typeof(TArg);
        _expression = expression;
        _usesDefaultTypePredicate = true;
    }

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
        : base(isPure)
    {
        _canHandle = p => p.ParameterType == typeof(TArg);
        _expression = expression;
        _usesDefaultTypePredicate = true;
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
        : base(isPure: false)
    {
        _expression = expression;
        _canHandle = canHandle;
        _usesDefaultTypePredicate = false;
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
    /// <param name="isPure">
    /// Defines if the parameter expression can be used for pure resolvers.
    /// </param>
    internal CustomParameterExpressionBuilder(
        Expression<Func<IResolverContext, TArg>> expression,
        Func<ParameterInfo, bool> canHandle,
        bool isPure)
        : base(isPure)
    {
        _expression = expression;
        _canHandle = canHandle;
        _usesDefaultTypePredicate = false;
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

    public override bool CanHandle(ParameterDescriptor parameter)
        => _usesDefaultTypePredicate && parameter.Type == typeof(TArg);

    internal override bool RequiresParameterInfo(ParameterDescriptor parameter)
        // A custom ParameterInfo predicate can match any parameter a TArg value is assignable to
        // (e.g. an interface or base type), not just an exact-TArg parameter. The source generator
        // cannot evaluate the predicate, so flag those parameters to fail loudly rather than
        // silently fall through to argument binding.
        => !_usesDefaultTypePredicate && parameter.Type.IsAssignableFrom(typeof(TArg));

    public override IParameterBinding Create(ParameterDescriptor parameter)
        => new CustomBinding(_compiled ??= _expression.Compile(), _isPure);

    private sealed class CustomBinding(
        Func<IResolverContext, TArg> compiled,
        bool isPure)
        : IParameterBinding
    {
        public bool IsPure => isPure;

        public T Execute<T>(IResolverContext context)
            => (T)(object)compiled(context)!;
    }
}
