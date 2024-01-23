using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
    public QueryableFilterProvider()
    {
    }

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
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        var applyFilter = CreateApplicator<TEntityType>(argumentName);

        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(FieldDelegate next, IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(ContextApplyFilteringKey, applyFilter);

            // first we let the pipeline run and produce a result.
            await next(context).ConfigureAwait(false);

            context.Result = applyFilter(context, context.Result);
        }
    }

    /// <inheritdoc />
    public override void ConfigureField(
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
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

        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitFilterArgument)VisitFilterArgumentExecutor;
        contextData[ContextVisitFilterArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
    }

    /// <inheritdoc />
    public override IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        if (fieldDefinition.Expression is not null)
        {
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

        return null;
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
        if (input is QueryableExecutable<TEntityType> { InMemory: var inMemory, })
        {
            return inMemory;
        }

        return input is not IQueryable || input is EnumerableQuery;
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
            QueryableExecutable<TEntityType> q => q.WithSource(q.Source.Where(where)),
            _ => input
        };

    private ApplyFiltering CreateApplicator<TEntityType>(string argumentName)
    {
        return (context, input) =>
        {
            // next we get the filter argument. If the filter argument is already on the context
            // we use this. This enabled overriding the context with LocalContextData
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.LocalContextData.ContainsKey(ContextValueNodeKey) &&
                context.LocalContextData[ContextValueNodeKey] is IValueNode node
                    ? node
                    : context.ArgumentLiteral<IValueNode>(argumentName);

            // if no filter is defined we can stop here and yield back control.
            var skipFiltering =
                context.LocalContextData.TryGetValue(SkipFilteringKey, out var skip) &&
                skip is true;

            // ensure filtering is only applied once
            context.LocalContextData =
                context.LocalContextData.SetItem(SkipFilteringKey, true);

            if (filter.IsNull() || skipFiltering)
            {
                return input;
            }

            if (argument.Type is IFilterInputType filterInput &&
                context.Selection.Field.ContextData
                    .TryGetValue(ContextVisitFilterArgumentKey, out var executorObj) &&
                executorObj is VisitFilterArgument executor)
            {
                var inMemory = IsInMemoryQuery<TEntityType>(input);

                var visitorContext = executor(filter, filterInput, inMemory);

                // compile expression tree
                if (visitorContext.TryCreateLambda(
                        out Expression<Func<TEntityType, bool>>? where))
                {
                    input = ApplyToResult(input, where);
                }
                else
                {
                    if (visitorContext.Errors.Count > 0)
                    {
                        input = Array.Empty<TEntityType>();
                        foreach (var error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                }
            }

            return input;
        };
    }
}
