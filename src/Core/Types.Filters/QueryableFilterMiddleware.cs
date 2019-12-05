using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterMiddleware<T>
    {
        private readonly FieldDelegate _next;
        private readonly ITypeConversion _converter;
        private readonly FilterMiddlewareContext _contextData;

        public QueryableFilterMiddleware(
            FieldDelegate next,
            FilterMiddlewareContext contextData,
            ITypeConversion converter)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _converter = converter ?? TypeConversion.Default;
            _contextData = contextData
                 ?? throw new ArgumentNullException(nameof(contextData));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false); 
            IValueNode filter = context.Argument<IValueNode>(_contextData.ArgumentName); 

            if (filter is null || filter is NullValueNode)
            {
                return;
            }

            IQueryable<T> source = null;

            if (context.Result is PageableData<T> p)
            {
                source = p.Source;
            }
            else
            {
                p = null;
            }

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (source != null
                && context.Field.Arguments[_contextData.ArgumentName].Type is InputObjectType iot
                && iot is IFilterInputType fit)
            {
                var visitor = new QueryableFilterVisitor(iot, fit.EntityType, _converter);
                filter.Accept(visitor);

                if (source is EnumerableQuery)
                {
                    source = source.Where(visitor.CreateFilterInMemory<T>());
                }
                else
                {
                    source = source.Where(visitor.CreateFilter<T>());
                }

                context.Result = p is null
                    ? (object)source
                    : new PageableData<T>(source, p.Properties);
            }
        }
    }
}
