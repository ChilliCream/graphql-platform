using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data;

internal static class ErrorHelper
{
    public static IError CreateNonNullError<T>(
        IFilterField field,
        IFilterVisitorContext<T> context)
    {
        var filterType = context.Types.OfType<IFilterInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                DataResources.ErrorHelper_CreateNonNullError,
                context.Operations.Peek().Name,
                filterType.Print())
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("filterType", filterType.Print())
            .Build();
    }

    public static IError CouldNotCreateFilterForOperation<T>(
        IFilterField field,
        IFilterVisitorContext<T> context)
    {
        var filterType = context.Types.OfType<IFilterInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                DataResources.CouldNotCreateFilterForOperation,
                context.Operations.Peek().Name,
                filterType.Print())
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("filterType", filterType.Print())
            .Build();
    }

    public static IError CreateNonNullError<T>(
        ISortField field,
        ISortVisitorContext<T> context)
    {
        var sortType = context.Types.OfType<ISortInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                DataResources.ErrorHelper_CreateNonNullError,
                context.Fields.Peek().Name,
                sortType.Print())
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("sortType", sortType.Print())
            .Build();
    }
}
