using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data;

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
                DataResources.ErrorHelper_CreateNonNullError,
                context.Operations.Peek().Name,
                filterType.Print())
            .AddLocation(value)
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", expectedType.Print())
            .SetExtension("filterType", filterType.Print())
            .Build();
    }

    public static IError SortingVisitor_ListValues(ISortField field, ListValueNode node) =>
        ErrorBuilder.New()
            .SetMessage(
                DataResources.SortingVisitor_ListInput_AreNotSupported,
                field.DeclaringType.Name,
                field.Name)
            .AddLocation(node)
            .SetCode(ErrorCodes.Data.ListNotSupported)
            .SetExtension(nameof(field), field)
            .Build();

    public static IError CreateNonNullError<T>(
        ISortField field,
        IValueNode value,
        ISortVisitorContext<T> context)
    {
        var sortType = context.Types.OfType<ISortInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                DataResources.ErrorHelper_CreateNonNullError,
                context.Fields.Peek().Name,
                sortType.Print())
            .AddLocation(value)
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("sortType", sortType.Print())
            .Build();
    }

    public static ISchemaError ProjectionConvention_UnableToCreateFieldHandler(
        IProjectionProvider convention,
        Type fieldHandler) =>
        SchemaErrorBuilder.New()
            .SetMessage(
                DataResources.FilterProvider_UnableToCreateFieldHandler,
                fieldHandler.FullName ?? fieldHandler.Name,
                convention.GetType().FullName ?? convention.GetType().Name)
            .SetExtension(nameof(convention), convention)
            .SetExtension(nameof(fieldHandler), fieldHandler)
            .Build();

    public static IError ProjectionProvider_CouldNotProjectFiltering(IValueNode node) =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionProvider_CouldNotProjectFiltering)
            .AddLocation(node)
            .SetCode(ErrorCodes.Data.FilteringProjectionFailed)
            .Build();

    public static IError ProjectionProvider_CouldNotProjectSorting(IValueNode node) =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionProvider_CouldNotProjectSorting)
            .SetCode(ErrorCodes.Data.SortingProjectionFailed)
            .AddLocation(node)
            .Build();

    public static IError ProjectionVisitor_NodeFieldWasNotFound(IPageType pageType) =>
        ErrorBuilder.New()
            .SetMessage(DataResources.ProjectionVisitor_NodeFieldWasNotFound, pageType.Name)
            .SetCode(ErrorCodes.Data.NodeFieldWasNotFound)
            .Build();
}
