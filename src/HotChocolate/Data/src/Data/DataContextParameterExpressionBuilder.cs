using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Data;

internal sealed class DataContextParameterExpressionBuilder()
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly MethodInfo _createDataContext =
        typeof(DataContextParameterExpressionBuilder)
            .GetMethod(nameof(CreateDataContext), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, FactoryCacheEntry> _expressionCache = new();

    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType.IsGenericType &&
           parameter.ParameterType.GetGenericTypeDefinition() == typeof(DataContext<>);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var resolverContext = context.ResolverContext;
        var parameterType = context.Parameter.ParameterType;

        var factoryCacheEntry =
            _expressionCache.GetOrAdd(
                parameterType,
                type =>
                {
                    var entityType = type.GetGenericArguments()[0];
                    var factoryMethod = _createDataContext.MakeGenericMethod(entityType);
                    var factory = Expression.Call(factoryMethod, resolverContext);
                    return new FactoryCacheEntry(factoryMethod, factory);
                });

        if(factoryCacheEntry.Factory is not null)
        {
            return factoryCacheEntry.Factory;
        }

        var factory = Expression.Call(factoryCacheEntry.FactoryMethod, resolverContext);
        _expressionCache.TryUpdate(parameterType, factoryCacheEntry with { Factory = factory }, factoryCacheEntry);
        return factory;
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        var factoryCacheEntry =
            _expressionCache.GetOrAdd(
                typeof(T),
                type =>
                {
                    var entityType = type.GetGenericArguments()[0];
                    var factoryMethod = _createDataContext.MakeGenericMethod(entityType);
                    return new FactoryCacheEntry(factoryMethod);
                });
        return (T)factoryCacheEntry.FactoryMethod.Invoke(null, [context])!;
    }

    private static DataContext<T> CreateDataContext<T>(IResolverContext context)
    {
        var selection = context.Selection;
        var filterContext = context.GetFilterContext();
        var sortContext = context.GetSortingContext();

        return new DataContext<T>(
            selection.AsSelector<T>(),
            filterContext?.AsPredicate<T>(),
            sortContext?.AsSortDefinition<T>());
    }

    private record FactoryCacheEntry(MethodInfo FactoryMethod, Expression? Factory = null);
}
