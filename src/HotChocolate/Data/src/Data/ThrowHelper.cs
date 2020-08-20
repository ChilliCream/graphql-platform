using System;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using static HotChocolate.Data.DataResources;

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
                        member?.DeclaringType?.Name)
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
                        "There is now provider defined for the filter convention `{0}`.",
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
    }
}
