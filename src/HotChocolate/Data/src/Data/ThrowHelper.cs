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
                        "The type of the member {0} of the declaring type {1} is unknown",
                        member.Name,
                        member.DeclaringType?.Name)
                    .Build());

        public static SchemaException FilterConvention_OperationNameNotFound(int operation) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Operation with identifier {0} has no name defined. Add a name to the " +
                        "filter convention",
                        operation)
                    .Build());

        public static SchemaException FilterConvention_NoProviderFound(Type convention, string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "There is no provider defined for the filter convention `{0}`.",
                        convention.FullName ?? convention.Name)
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException FilterProvider_NoHandlersConfigured(
            IFilterProvider filterProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The filter provider `{0}` does not specify and field handler.",
                        filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                    .SetExtension(nameof(filterProvider), filterProvider)
                    .Build());

        public static SchemaException FilterInterceptor_NoHandlerFoundForField(
            FilterInputTypeDefinition type,
            FilterFieldDefinition field) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the field {0} of type {1} was no handler found.",
                        field.Name,
                        type.Name)
                    .Build());

        public static GraphQLException FilterConvention_CouldNotConvertValue(
            IValueNode node) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Filtering could not convert value into desired format.")
                    .AddLocation(node)
                    .Build());

        public static SchemaException FilterObjectFieldDescriptorExtensions_CannotInfer() =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The filter type cannot be inferred from `System.Object`.")
                    .SetCode(ErrorCodes.Filtering.FilterObjectType)
                    .Build());

        public static SchemaException FilterDescriptorContextExtensions_NoConvention(
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "No filter convention found for scope `{0}`.",
                        scope ?? "none")
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException SortProvider_NoFieldHandlersConfigured(
            ISortProvider filterProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The sort provider `{0}` does not specify and field handler.",
                        filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                    .SetExtension(nameof(filterProvider), filterProvider)
                    .Build());

        public static SchemaException SortProvider_NoOperationHandlersConfigured(
            ISortProvider sortProvider) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The sort provider `{0}` does not specify and operation handler.",
                        sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                    .SetExtension(nameof(sortProvider), sortProvider)
                    .Build());

        public static SchemaException SortDescriptorContextExtensions_NoConvention(
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "No sorting convention found for scope `{0}`.",
                        scope ?? "none")
                    .SetExtension(nameof(scope), scope)
                    .Build());
        public static SchemaException SortInterceptor_NoFieldHandlerFoundForField(
            SortInputTypeDefinition type,
            SortFieldDefinition field) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the field {0} of type {1} was no field handler found.",
                        field.Name,
                        type.Name)
                    .Build());
        public static SchemaException SortInterceptor_NoOperationHandlerFoundForValue(
            EnumTypeDefinition type,
            SortEnumValueDefinition value) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the value {0} of type {1} was no operation handler found.",
                        value.Name,
                        type.Name)
                    .Build());

        public static SchemaException SortConvention_NoProviderFound(Type convention, string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "There is no provider defined for the filter convention `{0}`.",
                        convention.FullName ?? convention.Name)
                    .SetExtension(nameof(scope), scope)
                    .Build());

        public static SchemaException SortConvention_TypeOfMemberIsUnknown(MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The type of the member {0} of the declaring type {1} is unknown",
                        member.Name,
                        member.DeclaringType?.Name)
                    .Build());

        public static SchemaException SortConvention_OperationNameNotFound(int operation) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Operation with identifier {0} has no name defined. Add a name to the " +
                        "filter convention",
                        operation)
                    .Build());
    }
}
