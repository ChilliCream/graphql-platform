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
                    filterType.Print())
                .AddLocation(value)
                .SetCode(ErrorCodes.Data.NonNullError)
                .SetExtension("expectedType", new NonNullType(field.Type).Print())
                .SetExtension("filterType", filterType.Print())
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
                    sortType.Print())
                .AddLocation(value)
                .SetCode(ErrorCodes.Data.NonNullError)
                .SetExtension("expectedType", new NonNullType(field.Type).Print())
                .SetExtension("sortType", sortType.Print())
                .Build();
        }
    }
}
