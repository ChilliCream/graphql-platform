using System;
using HotChocolate.Types;

namespace HotChocolate
{
    internal static class TypeResourceHelper
    {
        public static string Scalar_Cannot_Serialize(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            return $"{typeName} cannot serialize the given value.";
        }

        public static string Scalar_Cannot_Deserialize(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            return $"{typeName} cannot deserialize the given value.";
        }

        public static string Scalar_Cannot_ParseLiteral(
            string typeName, Type literalType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            if (literalType == null)
            {
                throw new ArgumentNullException(nameof(literalType));
            }

            return $"{typeName} cannot parse the given " +
                "literal of type `{literalType.Name}`.";
        }

        public static string Scalar_Cannot_ParseValue(
            string typeName, Type valueType)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            if (valueType == null)
            {
                throw new ArgumentNullException(nameof(valueType));
            }

            return $"{typeName} cannot parse the given " +
                $"value of type `{valueType.FullName}`.";
        }

        public static string String_Argument_NullOrEmpty(string parameterName)
        {
            const string text = "The `{0}` cannot be null or empty.";

            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(
                    string.Format(text, nameof(parameterName)),
                    nameof(parameterName));
            }

            return string.Format(text, parameterName);
        }

        public static string BooleanType_Description()
        {
            return "The `Boolean` scalar type represents `true` or `false`.";
        }

        public static string DateTimeType_Description()
        {
            return "The `DateTime` scalar represents an ISO-8601 " +
                "compliant date time type.";
        }

        public static string DateType_Description()
        {
            return "The `Date` scalar represents an ISO-8601 " +
                "compliant date type.";
        }

        public static string DecimalType_Description()
        {
            return "The built-in `Decimal` scalar type.";
        }

        public static string FloatType_Description()
        {
            return "The `Float` scalar type represents signed " +
                "double-precision fractional values as specified by " +
                "[IEEE 754](http://en.wikipedia.org/wiki/IEEE_floating_point).";
        }

        public static string IdType_Description()
        {
            return "The `ID` scalar type represents a unique identifier, " +
                "often used to refetch an object or as key for a cache. " +
                "The ID type appears in a JSON response as a String; " +
                "however, it is not intended to be human-readable. " +
                "When expected as an input type, any string " +
                "(such as `\"4\"`) or integer (such as `4`) input value " +
                "will be accepted as an ID.";
        }

        public static string ByteType_Description()
        {
            return "The `Byte` scalar type represents non-fractional " +
                "whole numeric values. Byte can represent values " +
                "between 0 and 255.";
        }

        public static string ShortType_Description()
        {
            return "The `Short` scalar type represents non-fractional signed " +
                "whole 16-bit numeric values. Short can represent values " +
                "between -(2^15) and 2^15 - 1.";
        }

        public static string IntType_Description()
        {
            return "The `Int` scalar type represents non-fractional signed " +
                "whole numeric values. Int can represent values between " +
                "-(2^31) and 2^31 - 1.";
        }

        public static string LongType_Description()
        {
            return "The `Long` scalar type represents non-fractional signed " +
                "whole 64-bit numeric values. Long can represent values " +
                "between -(2^63) and 2^63 - 1.";
        }

        public static string StringType_Description()
        {
            return "The `String` scalar type represents textual data, " +
                "represented as UTF-8 character sequences. " +
                "The String type is most often used by GraphQL to " +
                "represent free-form human-readable text.";
        }

        public static string NameType_Description()
        {
            return "The name scalar represents a valid GraphQL name " +
                "as specified in the spec and can be used to refer " +
                "to fields or types.";
        }

        public static string MultiplierPathType_Description()
        {
            return "The multiplier path scalar represents a valid GraphQL " +
                "multiplier path string.";
        }

        public static string Name_Cannot_BeEmpty()
        {
            return "The specified name cannot be empty.";
        }

        public static string Field_Cannot_ResolveType(
            string typeName, string fieldName,
            TypeReference typeReference)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (typeReference == null)
            {
                return $"{typeName}.{fieldName}: Cannot resolve type.";
            }

            string kind = typeReference.Context == TypeContext.Output
                ? "output" : "input";

            return $"{typeName}.{fieldName}: Cannot resolve " +
                $"{kind}-type `{typeReference}`";
        }

        public static string Reflection_MemberMust_BeMethodOrProperty(
            string fullTypeName)
        {
            return "The member expression must specify a property or method " +
                "that is public and that belongs to the " +
                $"type {fullTypeName}";
        }

        public static string Type_Name_IsNotValid(string typeName)
        {
            string name = typeName ?? "null";
            return $"`{name}` is not a valid " +
                "GraphQL type name.";
        }
    }
}
