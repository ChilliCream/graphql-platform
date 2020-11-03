using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionProvider
        : ProjectionProvider
    {
        public QueryableProjectionProvider()
        {
        }

        public QueryableProjectionProvider(
            Action<IProjectionProviderDescriptor> configure)
            : base(configure)
        {
        }

        public override FieldMiddleware CreateExecutor<TEntityType>()
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

                if (context.Result is not null)
                {
                    var visitorContext =
                        new QueryableProjectionContext(
                            context,
                            context.ObjectType,
                            context.Field.Type.UnwrapRuntimeType());
                    var visitor = new QueryableProjectionVisitor();
                    visitor.Visit(visitorContext);

                    Expression<Func<TEntityType, TEntityType>> projection =
                        visitorContext.Project<TEntityType>();

                    context.Result = context.Result switch
                    {
                        IQueryable<TEntityType> q => q.Select(projection),
                        IEnumerable<TEntityType> e => e.AsQueryable().Select(projection),
                        QueryableExecutable<TEntityType> ex =>
                            ex.WithSource(ex.Source.Select(projection)),
                        _ => context.Result
                    };
                }
            }
        }
    }
}
