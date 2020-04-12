using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionVisitorDefintion : FilterVisitorDefinitionBase
    {
        public IReadOnlyDictionary<FilterKind, (FilterFieldEnter? enter, FilterFieldLeave? leave)>
            FieldHandler
        {
            get; set;
        } = ImmutableDictionary<FilterKind, (FilterFieldEnter? enter, FilterFieldLeave? leave)>
            .Empty;

        public IReadOnlyDictionary<(FilterKind, FilterOperationKind), FilterOperationHandler>
            OperationHandler
        {
            get; set;
        } = ImmutableDictionary<(FilterKind, FilterOperationKind), FilterOperationHandler>.Empty;

        public override async Task ApplyFilter<T>(
            IFilterConvention filterConvention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context)
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

            if (source != null &&
                context.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                var visitorContext = new QueryableFilterVisitorContext(
                    iot, fit.EntityType, this, converter, source is EnumerableQuery);
                QueryableFilterVisitor.Default.Visit(filter, visitorContext);

                source = source.Where(visitorContext.CreateFilter<T>());

                context.Result = p is null
                    ? (object)source
                    : new PageableData<T>(source, p.Properties);
            }
        }
    }
}
