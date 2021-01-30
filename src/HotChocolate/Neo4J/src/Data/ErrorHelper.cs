using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J
{
    internal static class ErrorHelper
    {
        public static IError CreateNonNullError<T>(
            IFilterField field,
            IValueNode value,
            IFilterVisitorContext<T> context)
        {
            IFilterInputType filterType = context.Types.OfType<IFilterInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    Neo4JResources.ErrorHelper_Filtering_CreateNonNullError,
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetCode(ErrorCodes.Data.NonNullError)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static IError CreateNonNullError<T>(
            ISortField field,
            IValueNode value,
            ISortVisitorContext<T> context)
        {
            ISortInputType sortType = context.Types.OfType<ISortInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    Neo4JResources.ErrorHelper_Filtering_CreateNonNullError,
                    context.Fields.Peek().Name,
                    sortType.Visualize())
                .AddLocation(value)
                .SetCode(ErrorCodes.Data.NonNullError)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("sortType", sortType.Visualize())
                .Build();
        }
    }
}
