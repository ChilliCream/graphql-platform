using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Registers the expression builder that provides support for <see cref="FilterContext" />
/// </summary>
internal sealed class FilterContextParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private const string GetFilterContext =
        nameof(FilterContextResolverContextExtensions.GetFilterContext);

    private static readonly MethodInfo s_getFilterContextMethod =
        typeof(FilterContextResolverContextExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(method => method.Name.Equals(GetFilterContext, StringComparison.Ordinal));

    /// <inheritdoc />
    public ArgumentKind Kind => ArgumentKind.Service;

    /// <inheritdoc />
    public bool IsPure => false;

    /// <inheritdoc />
    public bool IsDefaultHandler => false;

    /// <inheritdoc />
    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(IFilterContext);

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Type == typeof(IFilterContext);

    /// <inheritdoc />
    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Call(s_getFilterContextMethod, context.ResolverContext);

    public IParameterBinding Create(ParameterDescriptor context)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(IFilterContext) == typeof(T));
        var filterContext = context.GetFilterContext()!;
        return Unsafe.As<IFilterContext, T>(ref filterContext);
    }
}
