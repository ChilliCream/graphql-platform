using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterVisitorDefinitionBase
    {
        public FilterConventionDefinition? Convention { get; set; }

        public abstract Task ApplyFilter<T>(
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context);
    }
}
