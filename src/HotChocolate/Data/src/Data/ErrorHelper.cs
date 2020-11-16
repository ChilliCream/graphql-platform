using System;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data
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
                    DataResources.ErrorHelper_CreateNonNullError,
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static IError SortingVisitor_ListValues(ISortField field, ListValueNode node) =>
            ErrorBuilder.New()
                .SetMessage(
                    DataResources.SortingVisitor_ListInput_AreNotSuported,
                    field.DeclaringType.Name,
                    field.Name)
                .AddLocation(node)
                .SetExtension(nameof(field), field)
                .Build();

        public static IError CreateNonNullError<T>(
            ISortField field,
            IValueNode value,
            ISortVisitorContext<T> context)
        {
            ISortInputType sortType = context.Types.OfType<ISortInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    DataResources.ErrorHelper_CreateNonNullError,
                    context.Fields.Peek().Name,
                    sortType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("sortType", sortType.Visualize())
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

        public static IError ProjectionProvider_CreateMoreThanOneError(IResolverContext context) =>
            ErrorBuilder.New()
                .SetMessage(DataResources.ProjectionProvider_CreateMoreThanOneError)
                .SetCode("SELECTIONS_SINGLE_MORE_THAN_ONE")
                .SetPath(context.Path)
                .AddLocation(context.FieldSelection)
                .Build();

        public static IError ProjectionProvider_CreateMoreThanOneError() =>
            ErrorBuilder.New()
                .SetMessage(DataResources.ProjectionProvider_CreateMoreThanOneError)
                .SetCode("SELECTIONS_SINGLE_MORE_THAN_ONE")
                .Build();

        public static IError ProjectionProvider_CouldNotProjectFiltering(
            IValueNode node) =>
            ErrorBuilder.New()
                .SetMessage(DataResources.ProjectionProvider_CouldNotProjectFiltering)
                .AddLocation(node)
                .Build();

        public static IError ProjectionProvider_CouldNotProjectSorting(
            IValueNode node) =>
            ErrorBuilder.New()
                .SetMessage(DataResources.ProjectionProvider_CouldNotProjectSorting)
                .AddLocation(node)
                .Build();
    }
}
