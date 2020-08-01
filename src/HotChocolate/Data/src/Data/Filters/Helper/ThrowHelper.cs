using System;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters
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

        public static SchemaException FilterConvention_NoProviderFound(string scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the convention of scope {0} is no provider defined",
                        scope)
                    .Build());

        public static SchemaException FilterConvention_NoArgumentNameDefined(string scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the convention of scope {0} is no argument name defined",
                        scope)
                    .Build());

        public static SchemaException FilterConvention_NoVisitor(string scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the provider of scope {0} is no visitor defined",
                        scope)
                    .Build());

        public static SchemaException FilterConvention_NoCombinatorFound(string scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "For the provider of scope {0} is no combinator defined",
                        scope)
                    .Build());

        public static SchemaException FilterConvention_CombinatorOfWrongType<T, TContext>(
            string scope,
            FilterOperationCombinator combinator)
        {
            Type type = combinator.GetType() ??
                throw new ArgumentException("Type of combinator is invalid");

            while (type.BaseType != typeof(FilterOperationCombinator))
            {
                type = type.BaseType ??
                    throw new ArgumentException("Type of combinator is invalid");
            }

            return new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The combinator for the filter provider of scope {0} has the wrong type. " +
                        "The operation should be of type {1} and the context of type {2}" +
                        " but was of type {3} and the context of type {4} instead",
                        scope,
                        typeof(T).Name,
                        typeof(TContext).Name,
                        type.GenericTypeArguments[0].Name,
                        type.GenericTypeArguments[1].Name)
                .Build());
        }

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
                    .SetMessage("Filtering could not convert value into desired format")
                    .AddLocation(node)
                    .Build());
    }
}