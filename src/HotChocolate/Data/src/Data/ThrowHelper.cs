using System;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data
{
    internal static class ThrowHelper
    {
        public static SchemaException FilterConvention_TypeOfMemberIsUnknown(MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterConvention_TypeOfMemberIsUnknown,
                        member.Name,
                        member.DeclaringType?.Name)
                    .Build());

        public static SchemaException FilterConvention_OperationNameNotFound(int operation) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterConvention_OperationNameNotFound,
                        operation)
                    .Build());

        public static SchemaException FilterConvention_NoProviderFound(
            Type convention,
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterConvention_NoProviderFound,
                        convention.FullName ?? convention.Name)
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException FilterProvider_NoHandlersConfigured(
            IFilterProvider filterProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterProvider_NoHandlersConfigured,
                        filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                    .SetExtension(nameof(filterProvider), filterProvider)
                    .Build());

        public static SchemaException FilterInterceptor_NoHandlerFoundForField(
            FilterInputTypeDefinition type,
            FilterFieldDefinition field) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterInterceptor_NoHandlerFoundForField,
                        field.Name,
                        type.Name)
                    .Build());

        public static GraphQLException FilterConvention_CouldNotConvertValue(
            IValueNode node) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(DataResources.FilterConvention_CouldNotConvertValue)
                    .AddLocation(node)
                    .Build());

        public static SchemaException FilterObjectFieldDescriptorExtensions_CannotInfer() =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(DataResources.FilterObjectFieldDescriptorExtensions_CannotInfer)
                    .SetCode(ErrorCodes.Filtering.FilterObjectType)
                    .Build());

        public static SchemaException FilterDescriptorContextExtensions_NoConvention(
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterDescriptorContextExtensions_NoConvention,
                        scope ?? "none")
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException FilterProvider_UnableToCreateFieldHandler(
            IFilterProvider filterProvider,
            Type fieldHandler) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.FilterProvider_UnableToCreateFieldHandler,
                        fieldHandler.FullName ?? fieldHandler.Name,
                        filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                    .SetExtension(nameof(filterProvider), filterProvider)
                    .SetExtension(nameof(fieldHandler), fieldHandler)
                    .Build());

        public static SchemaException SortProvider_UnableToCreateFieldHandler(
            ISortProvider sortProvider,
            Type fieldHandler) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortProvider_UnableToCreateFieldHandler,
                        fieldHandler.FullName ?? fieldHandler.Name,
                        sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                    .SetExtension(nameof(sortProvider), sortProvider)
                    .SetExtension(nameof(fieldHandler), fieldHandler)
                    .Build());

        public static SchemaException SortProvider_UnableToCreateOperationHandler(
            ISortProvider sortProvider,
            Type operationHandler) =>
            new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_UnableToCreateOperationHandler,
                    operationHandler.FullName ?? operationHandler.Name,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .SetExtension(nameof(operationHandler), operationHandler)
                .Build());

        public static SchemaException SortProvider_NoFieldHandlersConfigured(
            ISortProvider filterProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortProvider_NoFieldHandlersConfigured,
                        filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                    .SetExtension(nameof(filterProvider), filterProvider)
                    .Build());

        public static SchemaException SortProvider_NoOperationHandlersConfigured(
            ISortProvider sortProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortProvider_NoOperationHandlersConfigured,
                        sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                    .SetExtension(nameof(sortProvider), sortProvider)
                    .Build());

        public static SchemaException SortDescriptorContextExtensions_NoConvention(
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortDescriptorContextExtensions_NoConvention,
                        scope ?? "none")
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException SortInterceptor_NoFieldHandlerFoundForField(
            SortInputTypeDefinition type,
            SortFieldDefinition field) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortInterceptor_NoFieldHandlerFoundForField,
                        field.Name,
                        type.Name)
                    .Build());

        public static SchemaException SortInterceptor_NoOperationHandlerFoundForValue(
            EnumTypeDefinition type,
            SortEnumValueDefinition value) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortInterceptor_NoOperationHandlerFoundForValue,
                        value.Name,
                        type.Name)
                    .Build());

        public static SchemaException SortConvention_NoProviderFound(
            Type convention,
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortConvention_NoProviderFound,
                        convention.FullName ?? convention.Name)
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException SortConvention_TypeOfMemberIsUnknown(MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortConvention_TypeOfMemberIsUnknown,
                        member.Name,
                        member.DeclaringType?.Name)
                    .Build());

        public static SchemaException SortConvention_OperationNameNotFound(int operation) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortConvention_OperationNameNotFound,
                        operation)
                    .Build());

        public static SchemaException SortObjectFieldDescriptorExtensions_CannotInfer() =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(DataResources.SortObjectFieldDescriptorExtensions_CannotInfer)
                    .SetCode(ErrorCodes.Filtering.FilterObjectType)
                    .Build());

        public static SchemaException SortConvention_OperationIsNotNamed(
            ISortConvention sortConvention,
            SortOperation sortOperation) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        DataResources.SortProvider_UnableToCreateFieldHandler,
                        sortOperation.Id,
                        sortConvention.GetType().FullName ?? sortConvention.GetType().Name,
                        sortConvention.Scope ?? "Default")
                    .SetExtension(nameof(sortConvention), sortConvention)
                    .SetExtension(nameof(sortOperation), sortOperation)
                    .Build());
    }
}
