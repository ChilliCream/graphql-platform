using System;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.DataResources;

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
                    "The provided value for filter `{0}` of type {1} is invalid. " +
                    "Null values are not supported.",
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static ISchemaError FilterField_RuntimeType_Unknown(FilterField field) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    FilterField_FilterField_TypeUnknown,
                    field.DeclaringType.Name,
                    field.Name)
                .SetTypeSystemObject(field.DeclaringType)
                .SetExtension(nameof(field), field)
                .Build();

        public static ISchemaError FilterProvider_UnableToCreateFieldHandler(
            IFilterProvider filterProvider,
            Type fieldHandler) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to create field handler `{0}` for filter provider `{1}`.",
                    fieldHandler.FullName ?? fieldHandler.Name,
                    filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                .SetExtension(nameof(filterProvider), filterProvider)
                .SetExtension(nameof(fieldHandler), fieldHandler)
                .Build();

        public static ISchemaError SortProvider_UnableToCreateFieldHandler(
            ISortProvider sortProvider,
            Type fieldHandler) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to create field handler `{0}` for sort provider `{1}`.",
                    fieldHandler.FullName ?? fieldHandler.Name,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .SetExtension(nameof(fieldHandler), fieldHandler)
                .Build();

        public static ISchemaError SortProvider_UnableToCreateOperationHandler(
            ISortProvider sortProvider,
            Type operationHandler) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to create operation handler `{0}` for sort provider `{1}`.",
                    operationHandler.FullName ?? operationHandler.Name,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .SetExtension(nameof(operationHandler), operationHandler)
                .Build();

        public static IError SortingVisitor_ListValues(ISortField field, ListValueNode node) =>
            ErrorBuilder.New()
                .SetMessage(
                    SortingVisitor_ListInput_AreNotSuported,
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
                    "The provided value for sorting `{0}` of type {1} is invalid. " +
                    "Null values are not supported.",
                    context.Fields.Peek().Name,
                    sortType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("sortType", sortType.Visualize())
                .Build();
        }
    }
}
