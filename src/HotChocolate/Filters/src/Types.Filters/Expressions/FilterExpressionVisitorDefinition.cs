using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionVisitorMiddleware : IFilterMiddleware<Expression>
    {
        public async Task ApplyFilter<T>(
            FilterVisitorDefinition<Expression> definition,
            IMiddlewareContext context,
            FieldDelegate next,
            IFilterConvention filterConvention,
            ITypeConversion converter,
            IFilterInputType fit,
            InputObjectType iot)
        {
            await next(context).ConfigureAwait(false);

            string argumentName = filterConvention!.GetArgumentName();

            IValueNode filter = context.Argument<IValueNode>(argumentName);

            if (filter is null || filter is NullValueNode)
            {
                return;
            }

            IQueryable<T>? source = null;
            PageableData<T>? p = null;
            if (context.Result is PageableData<T> pd)
            {
                source = pd.Source;
                p = pd;
            }

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source != null)
            {
                var visitorContext = new QueryableFilterVisitorContext(
                    fit, definition, converter, source is EnumerableQuery);
                FilterVisitor<Expression>.Default.Visit(filter, visitorContext);

                if (visitorContext.TryCreateLambda<T>(out Expression<Func<T, bool>>? where))
                {
                    source = source.Where(where);

                    context.Result = p is null
                        ? (object)source
                        : new PageableData<T>(source, p.Properties);
                }
                else
                {
                    if (visitorContext.Errors.Count > 0)
                    {
                        context.Result = Array.Empty<T>();
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                }
            }
        }

    }
}
