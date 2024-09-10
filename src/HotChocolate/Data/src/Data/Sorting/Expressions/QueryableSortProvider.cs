using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Sorting.Expressions;

/// <summary>
/// Applies the sorting to input
/// </summary>
[return: NotNullIfNotNull("input")]
public delegate object? ApplySorting(IResolverContext context, object? input);

/// <summary>
/// Visit the value node and returns the populated <see cref="QueryableSortContext"/>
/// </summary>
public delegate QueryableSortContext VisitSortArgument(
    IValueNode filterValueNode,
    ISortInputType filterInputType,
    bool inMemory);

/// <summary>
/// A <see cref="ISortProvider"/> for IQueryable
/// </summary>
public class QueryableSortProvider : SortProvider<QueryableSortContext>
{
    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that defines the name of the argument for sorting
    /// </summary>
    public const string ContextArgumentNameKey = "SortArgumentName";

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that holds the delegate which does the visitation of the sorting argument.
    /// <see cref="VisitSortArgument"/>
    /// </summary>
    public const string ContextVisitSortArgumentKey = nameof(VisitSortArgument);

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that holds the delegate which applies the sorting to input
    /// <see cref="ApplySorting"/>
    /// </summary>
    public const string ContextApplySortingKey = nameof(ApplySorting);

    /// <summary>
    /// The key for <see cref="IResolverContext.LocalContextData"/> on <see cref="IResolverContext"/>
    /// that defines if sorting is already applied and should be skipped
    /// </summary>
    public const string SkipSortingKey = "SkipSorting";

    /// <summary>
    /// The key for <see cref="IResolverContext.LocalContextData"/> on <see cref="IResolverContext"/>
    /// that holds the post sorting action.
    /// </summary>
    public const string PostSortingActionKey = "PostSortingAction";

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public QueryableSortProvider()
    {
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="configure">Configures the provider</param>
    public QueryableSortProvider(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to visit the input
    /// </summary>
    protected virtual SortVisitor<QueryableSortContext, QueryableSortOperation> Visitor { get; }
        = new();

    /// <inheritdoc />
    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new QueryableQueryBuilder(CreateApplicatorAsync<TEntityType>(argumentName.EnsureGraphQLName()));

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
        if (input is IQueryableExecutable<TEntityType> { IsInMemory: var inMemory, })
        {
            return inMemory;
        }

        return input is not IQueryable || input is EnumerableQuery;
    }

    /// <inheritdoc />
    public override void ConfigureField(
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
        QueryableSortContext VisitSortArgumentExecutor(
            IValueNode valueNode,
            ISortInputType filterInput,
            bool inMemory)
        {
            var visitorContext = new QueryableSortContext(filterInput, inMemory);

            // rewrite GraphQL input object into expression tree.
            Visitor.Visit(valueNode, visitorContext);

            return visitorContext;
        }

        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitSortArgument)VisitSortArgumentExecutor;
        contextData[ContextVisitSortArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
    }

    /// <inheritdoc />
    public override ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition)
    {
        if (fieldDefinition.Expression is not null)
        {
            if (fieldDefinition.Expression is not LambdaExpression lambda ||
                lambda.Parameters.Count != 1 ||
                lambda.Parameters[0].Type != typeDefinition.EntityType)
            {
                throw ThrowHelper.QueryableSortProvider_ExpressionParameterInvalid(
                    context.Type,
                    typeDefinition,
                    fieldDefinition);
            }

            return new ExpressionSortMetadata(fieldDefinition.Expression);
        }

        return null;
    }

    /// <summary>
    /// Applies the sorting to the result
    /// </summary>
    /// <param name="input">The result that is on <see cref="IResolverContext"/></param>
    /// <param name="sort">The sort expression</param>
    /// <typeparam name="TEntityType">The runtime type of the list element of the resolver</typeparam>
    /// <returns>The input combined with the sorting</returns>
    protected virtual object? ApplyToResult<TEntityType>(
        object? input,
        Func<IQueryable<TEntityType>, IQueryable<TEntityType>> sort)
        => input switch
        {
            IQueryable<TEntityType> q => sort(q),
            IEnumerable<TEntityType> q => sort(q.AsQueryable()),
            IQueryableExecutable<TEntityType> q => q.WithSource(sort(q.Source)),
            _ => input,
        };

    private object? ApplyPostActionToResult<TEntityType>(
        object? input,
        bool sortingApplied,
        PostSortingAction<IQueryable<TEntityType>> postAction)
        => input switch
        {
            IQueryable<TEntityType> q => postAction(sortingApplied, q),
            IEnumerable<TEntityType> q => postAction(sortingApplied, q.AsQueryable()),
            IQueryableExecutable<TEntityType> q => q.WithSource(postAction(sortingApplied, q.Source)),
            _ => input,
        };

    private ApplySorting CreateApplicatorAsync<TEntityType>(string argumentName)
        => (context, input) =>
        {
            // next we get the sort argument.
            var argument = context.Selection.Field.Arguments[argumentName];
            var sort = context.ArgumentLiteral<IValueNode>(argumentName);

            // if no sort is defined we can stop here and yield back control.
            var skipSorting = context.GetLocalStateOrDefault<bool>(SkipSortingKey);
            var postSortingAction = context.GetLocalStateOrDefault<PostSortingAction<IQueryable<TEntityType>>?>(PostSortingActionKey);

            // ensure sorting is only applied once
            context.SetLocalState(SkipSortingKey, true);

            if (sort.IsNull() || skipSorting)
            {
                return postSortingAction is not null
                    ? ApplyPostActionToResult(input, false, postSortingAction)
                    : input;
            }

            var sortingIsDefined = false;
            if (argument.Type is ListType lt &&
                lt.ElementType is NonNullType nn &&
                nn.NamedType() is ISortInputType sortInput &&
                context.Selection.Field.ContextData.TryGetValue(ContextVisitSortArgumentKey, out var executorObj) &&
                executorObj is VisitSortArgument executor)
            {
                var inMemory = IsInMemoryQuery<TEntityType>(input);

                var visitorContext = executor(sort, sortInput, inMemory);

                // compile expression tree
                if (visitorContext.Errors.Count == 0)
                {
                    input = ApplyToResult<TEntityType>(input, q => visitorContext.Sort(q));
                    sortingIsDefined = true;
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

            if (postSortingAction is not null)
            {
                input = ApplyPostActionToResult(input, sortingIsDefined, postSortingAction);
            }

            return input;
        };

    private sealed class QueryableQueryBuilder(ApplySorting applicator) : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
            => context.SetLocalState(ContextApplySortingKey, applicator);

        public void Apply(IMiddlewareContext context)
            => context.Result = applicator(context, context.Result);
    }
}
