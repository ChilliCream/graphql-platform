using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting.Conventions
{
    public abstract class SortingVisitorDefinitionBase
    {
        public abstract Task ApplSorting<T>(
            ISortingConvention convention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context);
    }
}
