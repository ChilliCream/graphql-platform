using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Registers the expression builder that provides support for <see cref="SortingContext" />
/// </summary>
internal sealed class SortingContextParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private const string _getSortingContext =
        nameof(SortingContextResolverContextExtensions.GetSortingContext);

    private static readonly MethodInfo _getSortingContextMethod =
        typeof(SortingContextResolverContextExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(method => method.Name.Equals(_getSortingContext, StringComparison.Ordinal));

    /// <inheritdoc cref="IParameterExpressionBuilder.Kind" />
    public ArgumentKind Kind => ArgumentKind.Service;

    /// <inheritdoc cref="IParameterExpressionBuilder.IsPure" />
    public bool IsPure => false;

    /// <inheritdoc />
    public bool IsDefaultHandler => false;

    /// <inheritdoc />
    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ISortingContext);

    /// <inheritdoc />
    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Call(_getSortingContextMethod, context.ResolverContext);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)context.GetSortingContext()!;
}
