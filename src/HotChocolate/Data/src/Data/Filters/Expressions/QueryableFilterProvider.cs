using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

/// <summary>
/// Applies the filtering to input
/// </summary>
[return: NotNullIfNotNull("input")]
public delegate object? ApplyFiltering(IResolverContext context, object? input);

public delegate Expression<Func<TEntityType, bool>>? AsPredicate<TEntityType>(
    IResolverContext context,
    bool isInMemory);

/// <summary>
/// Visit the value node and returns the populated <see cref="QueryableFilterContext"/>
/// </summary>
public delegate QueryableFilterContext VisitFilterArgument(
    IValueNode filterValueNode,
    IFilterInputType filterInputType,
    bool inMemory);

/// <summary>
/// A <see cref="IFilterProvider"/> for IQueryable
/// </summary>
public class QueryableFilterProvider : FilterProvider<QueryableFilterContext>
{
    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that defines the name of the argument for filtering
    /// </summary>
    public static readonly string ContextArgumentNameKey = "FilterArgumentName";

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that holds the delegate which does the visitation of the filtering argument.
    /// <see cref="VisitFilterArgument"/>
    /// </summary>
    public static readonly string ContextVisitFilterArgumentKey = nameof(VisitFilterArgument);

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that holds the delegate which applies the filtering to input
    /// <see cref="ApplyFiltering"/>
    /// </summary>
    public static readonly string ContextApplyFilteringKey = nameof(ApplyFiltering);

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that defines if the filter should be applied as a predicate
    /// </summary>
    public static readonly string ContextAsPredicateKey = nameof(AsPredicate);

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that defines if filtering is already applied and should be skipped
    /// </summary>
    public static readonly string SkipFilteringKey = "SkipFiltering";

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that stores a custom value node that is used instead of the value node on the argument
    /// </summary>
    public static readonly string ContextValueNodeKey = nameof(QueryableFilterProvider);

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public QueryableFilterProvider() { }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="configure">Configures the provider</param>
    public QueryableFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to visit the input
    /// </summary>
    protected virtual FilterVisitor<QueryableFilterContext, Expression> Visitor { get; } =
        new(new QueryableCombinator());

    /// <inheritdoc />
    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new QueryableQueryBuilder<TEntityType>(
            CreateApplicator<TEntityType>(argumentName),
            (ctx, inMemory) => AsPredicate<TEntityType>(ctx, argumentName, inMemory));

    /// <inheritdoc />
    public override void ConfigureField(
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitFilterArgument)VisitFilterArgumentExecutor;
        contextData[ContextVisitFilterArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
        return;

        QueryableFilterContext VisitFilterArgumentExecutor(
            IValueNode valueNode,
            IFilterInputType filterInput,
            bool inMemory)
        {
            var visitorContext = new QueryableFilterContext(filterInput, inMemory);

            // rewrite GraphQL input object into expression tree.
            Visitor.Visit(valueNode, visitorContext);

            return visitorContext;
        }
    }

    /// <inheritdoc />
    public override IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        if (fieldDefinition.Expression is null)
        {
            return null;
        }

        if (fieldDefinition.Expression is not LambdaExpression lambda ||
            lambda.Parameters.Count != 1 ||
            lambda.Parameters[0].Type != typeDefinition.EntityType)
        {
            throw ThrowHelper.QueryableFilterProvider_ExpressionParameterInvalid(
                context.Type,
                typeDefinition,
                fieldDefinition);
        }

        return new ExpressionFilterMetadata(fieldDefinition.Expression);
    }

    /// <summary>
    /// Checks if the input has to be computed in memory. Null checks are only applied when the
    /// query is executed in memory
    /// </summary>
    /// <param name="input">The result that is on <see cref="IResolverContext"/></param>
    /// <typeparam name="TEntityType">
    /// The runtime type of the list element of the resolver
    /// </typeparam>
    /// <returns>
    /// <c>true</c> when the <paramref name="input"/> is in memory, otherwise<c>false</c>
    /// </returns>
    protected virtual bool IsInMemoryQuery<TEntityType>(object? input)
    {
        return input is IQueryableExecutable<TEntityType> { IsInMemory: var inMemory, }
            ? inMemory
            : input is not IQueryable or EnumerableQuery;
    }

    /// <summary>
    /// Applies the filtering to the result
    /// </summary>
    /// <param name="input">The result that is on <see cref="IResolverContext"/></param>
    /// <param name="where">The filter expression</param>
    /// <typeparam name="TEntityType">The runtime type of the list element of the resolver</typeparam>
    /// <returns>The input combined with the filtering</returns>
    protected virtual object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, bool>> where)
        => input switch
        {
            IQueryable<TEntityType> q => q.Where(where),
            IEnumerable<TEntityType> q => q.AsQueryable().Where(where),
            IQueryableExecutable<TEntityType> q => q.WithSource(q.Source.Where(where)),
            _ => input,
        };

    private ApplyFiltering CreateApplicator<TEntityType>(string argumentName)
        => (context, input) =>
        {
            var inMemory = IsInMemoryQuery<TEntityType>(input);

            // if no filter is defined we can stop here and yield back control.
            var skipFiltering = context.GetLocalStateOrDefault<bool>(SkipFilteringKey);

            // ensure filtering is only applied once
            context.SetLocalState(SkipFilteringKey, true);

            if (skipFiltering)
            {
                return input;
            }

            var predicate = AsPredicate<TEntityType>(context, argumentName, inMemory);

            if (predicate is not null)
            {
                input = ApplyToResult(input, predicate);
            }

            return input;
        };

    private Expression<Func<TEntityType, bool>>? AsPredicate<TEntityType>(
        IResolverContext context,
        string argumentName,
        bool isInMemory)
    {
        // next we get the filter argument. If the filter argument is already on the context
        // we use this. This enabled overriding the context with LocalContextData
        var argument = context.Selection.Field.Arguments[argumentName];
        var filter = context.GetLocalStateOrDefault<IValueNode>(ContextValueNodeKey) ??
            context.ArgumentLiteral<IValueNode>(argumentName);

        if (filter.IsNull())
        {
            return null;
        }

        if (argument.Type is IFilterInputType filterInput &&
            context.Selection.Field.ContextData.TryGetValue(ContextVisitFilterArgumentKey,
                out var executorObj) &&
            executorObj is VisitFilterArgument executor)
        {
            var visitorContext = executor(filter, filterInput, isInMemory);

            // compile expression tree
            if (visitorContext.Errors.Count == 0)
            {
                // if we have an empty filter object it might be that there is no lambda that needs to be applied.
                // this depends on the provider implementation.
                if (visitorContext.TryCreateLambda(out Expression<Func<TEntityType, bool>>? where))
                {
                    return where;
                }
            }
            else
            {
                var exceptions = new List<GraphQLException>(visitorContext.Errors.Count);

                foreach (var error in visitorContext.Errors)
                {
                    exceptions.Add(new GraphQLException(error));
                }

                throw new AggregateException(exceptions);
            }
        }

        return null;
    }

    private sealed class QueryableQueryBuilder<T>(
        ApplyFiltering applicator,
        AsPredicate<T> asPredicate)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            context.SetLocalState(ContextApplyFilteringKey, applicator);
            context.SetLocalState(ContextAsPredicateKey, asPredicate);
        }

        public void Apply(IMiddlewareContext context)
            => context.Result = applicator(context, context.Result);
    }
}
