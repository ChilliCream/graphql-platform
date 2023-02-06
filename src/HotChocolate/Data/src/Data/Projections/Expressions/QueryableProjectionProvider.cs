using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions;

public delegate object? ApplyProjection(IResolverContext context, object? input);

public class QueryableProjectionProvider : ProjectionProvider
{
    public static readonly string ContextApplyProjectionKey = nameof(ApplyProjection);
    public const string SkipProjectionKey = "SkipProjection";

    public QueryableProjectionProvider()
    {
    }

    public QueryableProjectionProvider(Action<IProjectionProviderDescriptor> configure)
        : base(configure)
    {
    }

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

            context.Result = applyProjection(context, context.Result);
        }
    }

    private ApplyProjection CreateApplicator<TEntityType>()
        => (context, input) =>
        {
            if (input is null)
            {
                return input;
            }

            // if projections are already applied we can skip
            var skipProjection =
                context.LocalContextData.TryGetValue(SkipProjectionKey, out var skip) &&
                skip is true;

            // ensure sorting is only applied once
            context.LocalContextData =
                context.LocalContextData.SetItem(SkipProjectionKey, true);

            if (skipProjection)
            {
                return input;
            }

            var inMemory = IsInMemoryQuery<TEntityType>(input);

            var visitorContext = new QueryableProjectionContext(
                context,
                context.ObjectType,
                context.Selection.Type.UnwrapRuntimeType(),
                inMemory);

            var visitor = new QueryableProjectionVisitor();

            visitor.Visit(visitorContext);

            var projection = visitorContext.Project<TEntityType>();

            return ApplyToResult(input, projection);
        };

    protected virtual bool IsInMemoryQuery<TEntityType>(object? input)
    {
        // We cannot opt out of the nullchecks because ef core does not like it
        return true;
    }

    protected virtual object? ApplyToResult<TEntityType>(
        object? input,
        Expression<Func<TEntityType, TEntityType>> projection)
        => input switch
        {
            IQueryable<TEntityType> q => q.Select(projection),
            IEnumerable<TEntityType> e => e.AsQueryable().Select(projection),
            QueryableExecutable<TEntityType> ex => ex.WithSource(ex.Source.Select(projection)),
            _ => input
        };
}
