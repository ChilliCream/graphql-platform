using System;
using System.Globalization;

namespace HotChocolate
{
    internal static class TypeResources
    {
        public static string Scalar_Cannot_Serialize(NameString typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            return $"{typeName} cannot serialize the given value.";
        }

        public static string Scalar_Cannot_Deserialize(NameString typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            return $"{typeName} cannot deserialize the given value.";
        }

        public static string Scalar_Cannot_ParseLiteral(
            NameString typeName, Type literalType)
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
            NameString typeName, Type valueType)
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

        public static string Type_Name_IsNotValid(NameString typeName)
        {
            string name = typeName.HasValue ? typeName.Value : "null";
            return $"`{name}` is not a valid " +
                "GraphQL type name.";
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

        public static string IntType_Description()
        {
            return "The `Int` scalar type represents non-fractional signed " +
                "whole numeric values. Int can represent values between " +
                "-(2^31) and 2^31 - 1.";
        }

        public static string StringType_Description()
        {
            return "The `String` scalar type represents textual data, " +
                "represented as UTF-8 character sequences. " +
                "The String type is most often used by GraphQL to " +
                "represent free-form human-readable text.";
        }

        public static string Name_CannotBe_Empty()
        {
            return "The specified name cannot be empty.";
        }

    }
}
