using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data;

internal static class ErrorHelper
{
    public static IError CreateNonNullError<T>(
        IFilterField field,
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
                DataResources.ErrorHelper_CreateNonNullError,
                context.Operations.Peek().Name,
                filterType.Print())
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", expectedType.Print())
            .SetExtension("filterType", filterType.Print())
            .Build();
    }

    public static IError SortingVisitor_ListValues(ISortField field) =>
        ErrorBuilder.New()
            .SetMessage(
                DataResources.SortingVisitor_ListInput_AreNotSupported,
                field.DeclaringType.Name,
                field.Name)
            .SetCode(ErrorCodes.Data.ListNotSupported)
            .SetExtension(nameof(field), field)
            .Build();

    public static IError MaxAllowedFilterOperationsExceeded(
        int filterOperations,
        int maxAllowedFilterOperations) =>
        ErrorBuilder.New()
            .SetMessage(
                "The filter argument contains {0} operations, which exceeds the maximum allowed "
                + "number of {1}.",
                filterOperations,
                maxAllowedFilterOperations)
            .SetCode(ErrorCodes.Data.MaxFilterOperationsExceeded)
            .SetExtension(nameof(filterOperations), filterOperations)
            .SetExtension(nameof(maxAllowedFilterOperations), maxAllowedFilterOperations)
            .Build();

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
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("sortType", sortType.Print())
            .Build();
    }

    public static ISchemaError ProjectionConvention_UnableToCreateFieldHandler(
        IProjectionProvider convention,
        Exception exception) =>
        SchemaErrorBuilder.New()
            .SetMessage(
                DataResources.ProjectionProvider_UnableToCreateFieldHandler,
                convention.GetType().FullName ?? convention.GetType().Name)
            .SetExtension(nameof(convention), convention)
            .SetException(exception)
            .Build();

    public static IError ProjectionProvider_CouldNotProjectFiltering() =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionProvider_CouldNotProjectFiltering)
            .SetCode(ErrorCodes.Data.FilteringProjectionFailed)
            .Build();

    public static IError ProjectionProvider_CouldNotProjectSorting() =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionProvider_CouldNotProjectSorting)
            .SetCode(ErrorCodes.Data.SortingProjectionFailed)
            .Build();

    public static IError ProjectionVisitor_NodeFieldWasNotFound(IPageType pageType) =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionVisitor_NodeFieldWasNotFound, pageType.Name)
            .SetCode(ErrorCodes.Data.NodeFieldWasNotFound)
            .Build();
}
