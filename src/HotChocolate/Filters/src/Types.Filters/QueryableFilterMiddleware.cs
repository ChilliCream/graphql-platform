using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly ITypeConverter _converter;
        private readonly FilterMiddlewareContext _contextData;

        public QueryableFilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext contextData,
            ITypeConverter converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _converter = converter ?? DefaultTypeConverter.Default;
            _contextData = contextData
                 ?? throw new ArgumentNullException(nameof(contextData));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IValueNode filter = context.ArgumentLiteral<IValueNode>(_contextData.ArgumentName);

            if (filter is NullValueNode)
            {
                return;
            }

            IQueryable<T>? source = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source is not null &&
                context.Field.Arguments[_contextData.ArgumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {

                var visitorContext = new QueryableFilterVisitorContext(
                    iot, fit.EntityType, _converter, source is EnumerableQuery);
                QueryableFilterVisitor.Default.Visit(filter, visitorContext);

                source = source.Where(visitorContext.CreateFilter<T>());

                context.Result = source;
            }
        }
    }
}
