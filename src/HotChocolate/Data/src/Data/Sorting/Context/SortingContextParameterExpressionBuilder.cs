using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Registers the expression builder that provides support for <see cref="SortingContext" />
/// </summary>
internal sealed class SortingContextParameterExpressionBuilder
    : IParameterExpressionBuilder
{
    private const string _getSortingContext =
        nameof(SortingContextResolverContextExtensions.GetSortingContext);

    private static readonly MethodInfo _getSortingContextMethod =
        typeof(SortingContextResolverContextExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(method => method.Name.Equals(_getSortingContext, StringComparison.Ordinal));

    /// <inheritdoc />
    public ArgumentKind Kind => ArgumentKind.Service;

    /// <inheritdoc />
    public bool IsPure => false;

    /// <inheritdoc />
    public bool IsDefaultHandler => false;

    /// <inheritdoc />
    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ISortingContext);

    /// <inheritdoc />
    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Call(_getSortingContextMethod, context.ResolverContext);
}
