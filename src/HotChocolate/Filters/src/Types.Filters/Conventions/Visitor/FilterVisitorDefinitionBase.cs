using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterVisitorDefinitionBase
    {
        public abstract Task ApplyFilter<TSource>(
            IFilterConvention convention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context);
    }
}
