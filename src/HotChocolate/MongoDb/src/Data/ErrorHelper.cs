using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb;

internal static class ErrorHelper
{
    public static IError CreateNonNullError<T>(
        IFilterField field,
        IValueNode value,
        IFilterVisitorContext<T> context,
        bool isMemberInvalid = false)
    {
        var filterType = context.Types.OfType<IFilterInputType>().First();

        IType expectedType =
            isMemberInvalid && field.Type.IsListType()
                ? new ListType(new NonNullType(field.Type.ElementType()))
                : new NonNullType(field.Type);

        return ErrorBuilder.New()
            .SetMessage(
                MongoDbResources.ErrorHelper_Filtering_CreateNonNullError,
                context.Operations.Peek().Name,
                filterType.Print())
            .AddLocation(value)
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", expectedType.Print())
            .SetExtension("filterType", filterType.Print())
            .Build();
    }

    public static IError CreateNonNullError<T>(
        ISortField field,
        IValueNode value,
        ISortVisitorContext<T> context)
    {
        var sortType = context.Types.OfType<ISortInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                MongoDbResources.ErrorHelper_Filtering_CreateNonNullError,
                context.Fields.Peek().Name,
                sortType.Print())
            .AddLocation(value)
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("sortType", sortType.Print())
            .Build();
    }
}
