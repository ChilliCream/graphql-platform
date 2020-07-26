using System.Reflection;

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
    }
}