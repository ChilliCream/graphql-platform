using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions;

/// <summary>
/// Applies the projection to input
/// </summary>
[return: NotNullIfNotNull("input")]
public delegate TypedValue? ApplyProjection(IResolverContext context, TypedValue? input);

/// <summary>
/// A <see cref="IProjectionProvider"/> for IQueryable
/// </summary>
public class QueryableProjectionProvider : ProjectionProvider
{
    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that holds the delegate which applies the projection to input
    /// <see cref="ApplyProjection"/>
    /// </summary>
    public static readonly string ContextApplyProjectionKey = nameof(ApplyProjection);

    /// <summary>
    /// The key for <see cref="IHasContextData.ContextData"/> on <see cref="IResolverContext"/>
    /// that defines if projection is already applied and should be skipped
    /// </summary>
    public const string SkipProjectionKey = "SkipProjection";

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public QueryableProjectionProvider()
    {
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="configure">Configures the provider</param>
    public QueryableProjectionProvider(Action<IProjectionProviderDescriptor> configure)
        : base(configure)
    {
    }

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>()
    {
        var applyProjection = CreateApplicator<TEntityType>();

        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(ContextApplyProjectionKey, applyProjection);

            // first we let the pipeline run and produce a result.
            await next(context).ConfigureAwait(false);

            context.TypedResult = applyProjection(context, context.TypedResult);
        }
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
        // We cannot opt out of the null checks because ef core does not like it
        return true;
    }

    /// <summary>
    /// Applies the projection to the result
    /// </summary>
    /// <param name="input">The result that is on <see cref="IResolverContext"/></param>
    /// <param name="projection">The projected expression</param>
    /// <typeparam name="TEntityType">The runtime type of the list element of the resolver</typeparam>
    /// <returns>The input combined with the projection</returns>
    protected virtual object ApplyToResult<TEntityType>(
        object input,
        Expression<Func<TEntityType, object[]>> projection)
        => input switch
        {
            IQueryable<TEntityType> q => q.Select(projection),
            IEnumerable<TEntityType> q => q.AsQueryable().Select(projection),
            QueryableExecutable<TEntityType> q => new QueryableExecutable<object[]>(q.Source.Select(projection)),
            _ => input,
        };

    private ApplyProjection CreateApplicator<TEntityType>()
        => (context, input) =>
        {
            if (input is null)
            {
                return null;
            }

            // if projections are already applied we can skip
            var skipProjection =
                context.LocalContextData.TryGetValue(SkipProjectionKey, out var skip) &&
                skip is true;
            if (skipProjection)
            {
                return input;
            }

            // ensure projection is only applied once
            context.LocalContextData =
                context.LocalContextData.SetItem(SkipProjectionKey, true);

            var inMemory = IsInMemoryQuery<TEntityType>(input);

            var visitorContext = new QueryableProjectionContext(
                context,
                context.ObjectType,
                context.Selection.Type.UnwrapRuntimeType(),
                inMemory);

            var visitor = new QueryableProjectionVisitor();

            visitor.Visit(visitorContext);

            var projection = visitorContext.Project<TEntityType>();

            var resultObject = ApplyToResult(input.Value, projection);
            return new TypedValue(resultObject, typeof(TEntityType), isCollection: true);
        };
}
