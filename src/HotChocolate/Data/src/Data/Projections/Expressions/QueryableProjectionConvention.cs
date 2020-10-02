using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionConvention
        : ProjectionConvention
    {
        public QueryableProjectionConvention()
        {
        }

        public QueryableProjectionConvention(
            Action<IProjectionConventionDescriptor> configure)
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


                IQueryable<TEntityType>? source = null;

                if (context.Result is IQueryable<TEntityType> q)
                {
                    source = q;
                }
                else if (context.Result is IEnumerable<TEntityType> e)
                {
                    source = e.AsQueryable();
                }

                if (source is not null)
                {
                    var visitorContext =
                        new QueryableProjectionContext(context, context.ObjectType);
                    var visitor = new ProjectionVisitor<QueryableProjectionContext>();
                    visitor.Visit(visitorContext);
//                    context.Result = source.Select(visitor.Project<TEntityType>());
                }
            }
        }
    }
}
