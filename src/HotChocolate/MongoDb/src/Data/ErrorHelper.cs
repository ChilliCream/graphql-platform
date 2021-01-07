using System;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb
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
                    MongoDbResources.ErrorHelper_Filtering_CreateNonNullError,
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
                    MongoDbResources.ErrorHelper_Filtering_CreateNonNullError,
                    context.Fields.Peek().Name,
                    sortType.Visualize())
                .AddLocation(value)
                .SetCode(ErrorCodes.Data.NonNullError)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("sortType", sortType.Visualize())
                .Build();
        }
    }

    internal static class ThrowHelper
    {
        public static GraphQLException PagingTypeNotSupported(Type type)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        MongoDbResources.Paging_SourceIsNotSupported,
                        type.FullName ?? type.Name)
                    .SetCode(ErrorCodes.Data.NoPagingationProviderFound)
                    .Build());
        }
    }
}
