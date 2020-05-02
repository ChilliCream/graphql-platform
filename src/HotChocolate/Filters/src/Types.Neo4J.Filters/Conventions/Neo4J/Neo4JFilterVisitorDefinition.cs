using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Neo4J.Filters;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public class Neo4JFilterVisitorDefinition
        : FilterVisitorDefinitionBase
    {

        public override async Task ApplyFilter<T>(
            IFilterConvention filterConvention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context)
        {
            string argumentName = filterConvention!.GetArgumentName();

            IValueNode filter = context.Argument<IValueNode>(argumentName);

            if (!(filter is null) && !(filter is NullValueNode) &&
                context.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                var visitorContext = new Neo4JFilterVisitorContext(
                    iot, fit.EntityType, converter);

                Neo4JFilterVisitor.Default.Visit(filter, visitorContext);
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
