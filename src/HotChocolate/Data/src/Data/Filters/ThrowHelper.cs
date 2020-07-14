using System.Reflection;

namespace HotChocolate.Data.Filters
{
    internal static class ThrowHelper
    {
        public static SchemaException FilterConvention_TypeOfMemberisUnknown(MemberInfo member) =>
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
    }
}