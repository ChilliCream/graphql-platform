using System;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class StringExtensions
    {
        public static string ToFieldName(this NameString nameString) =>
            '_' + WithLowerFirstChar(nameString);

        public static string WithLowerFirstChar(this NameString nameString) =>
            char.ToLower(nameString.Value[0]) + nameString.Value.Substring(1);

        public static string WithLowerFirstChar(this string nameString) =>
            char.ToLower(nameString[0]) + nameString.Substring(1);

        public static string WithCapitalFirstChar(this NameString nameString) =>
            char.ToUpper(nameString.Value[0]) + nameString.Value.Substring(1);

        public static string WithCapitalFirstChar(this NameString? nameString)
        {
            if (nameString is null)
            {
                throw new ArgumentNullException(nameof(nameString));
            }

            return WithCapitalFirstChar(nameString);
        }
    }
}
