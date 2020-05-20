
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterMiddleware<T>
    {
        Task ApplyFilter<TSource>(
           FilterVisitorDefinition<T> definition,
           IMiddlewareContext context,
           FieldDelegate next,
           IFilterConvention filterConvention,
           ITypeConversion converter,
           IFilterInputType fit,
           InputObjectType iot);
    }
}
