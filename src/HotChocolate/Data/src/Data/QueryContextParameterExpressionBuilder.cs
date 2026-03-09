using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Data;

internal sealed class QueryContextParameterExpressionBuilder()
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly MethodInfo s_createQueryContext =
        typeof(QueryContextParameterExpressionBuilder)
            .GetMethod(nameof(CreateQueryContext), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, FactoryCacheEntry> s_expressionCache = new();

    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType.IsGenericType
            && parameter.ParameterType.GetGenericTypeDefinition() == typeof(QueryContext<>);

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Type.IsGenericType
            && parameter.Type.GetGenericTypeDefinition() == typeof(QueryContext<>);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var resolverContext = context.ResolverContext;
        var parameterType = context.Parameter.ParameterType;

        var factoryCacheEntry =
            s_expressionCache.GetOrAdd(
                parameterType,
                type =>
                {
                    var entityType = type.GetGenericArguments()[0];
                    var factoryMethod = s_createQueryContext.MakeGenericMethod(entityType);
                    var factory = Expression.Call(factoryMethod, resolverContext);
                    return new FactoryCacheEntry(factoryMethod, factory);
                });

        if (factoryCacheEntry.Factory is not null)
        {
            return factoryCacheEntry.Factory;
        }

        var factory = Expression.Call(factoryCacheEntry.FactoryMethod, resolverContext);
        s_expressionCache.TryUpdate(parameterType, factoryCacheEntry with { Factory = factory }, factoryCacheEntry);
        return factory;
    }

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        var factoryCacheEntry =
            s_expressionCache.GetOrAdd(
                typeof(T),
                type =>
                {
                    var entityType = type.GetGenericArguments()[0];
                    var factoryMethod = s_createQueryContext.MakeGenericMethod(entityType);
                    return new FactoryCacheEntry(factoryMethod);
                });
        return (T)factoryCacheEntry.FactoryMethod.Invoke(null, [context])!;
    }

    private static QueryContext<T> CreateQueryContext<T>(IResolverContext context)
    {
        var selection = context.Selection;
        var filterContext = context.GetFilterContext();
        var sortContext = context.GetSortingContext();

        return new QueryContext<T>(
            selection.AsSelector<T>(context.IncludeFlags),
            filterContext?.AsPredicate<T>(),
            sortContext?.AsSortDefinition<T>());
    }

    private record FactoryCacheEntry(MethodInfo FactoryMethod, Expression? Factory = null);
}
