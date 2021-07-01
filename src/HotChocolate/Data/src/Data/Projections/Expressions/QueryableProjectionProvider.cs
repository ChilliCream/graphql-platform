using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions
{
    public delegate object? ApplyProjection(IResolverContext context, object? input);

    public class QueryableProjectionProvider
        : ProjectionProvider
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
            ApplyProjection applyProjection = CreateApplicatorAsync<TEntityType>();

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

        private static ApplyProjection CreateApplicatorAsync<TEntityType>()
        {
            return (context, input) =>
            {
                if (input is null)
                {
                    return input;
                }

                // if projections are already applied we can skip
                var skipProjection =
                    context.LocalContextData.TryGetValue(SkipProjectionKey, out object? skip) &&
                    skip is true;

                // ensure sorting is only applied once
                context.LocalContextData =
                    context.LocalContextData.SetItem(SkipProjectionKey, true);

                if (skipProjection)
                {
                    return input;
                }

                var visitorContext =
                    new QueryableProjectionContext(
                        context,
                        context.ObjectType,
                        context.Field.Type.UnwrapRuntimeType());
                var visitor = new QueryableProjectionVisitor();
                visitor.Visit(visitorContext);

                Expression<Func<TEntityType, TEntityType>> projection =
                    visitorContext.Project<TEntityType>();

                input = input switch
                {
                    IQueryable<TEntityType> q => q.Select(projection),
                    IEnumerable<TEntityType> e => e.AsQueryable().Select(projection),
                    QueryableExecutable<TEntityType> ex =>
                        ex.WithSource(ex.Source.Select(projection)),
                    _ => input
                };

                return input;
            };
        }
    }
}
