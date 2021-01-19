using HotChocolate;

namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class StringExtensions
    {
        public static string ToFieldName(this NameString nameString)
        {
            return '_' + WithLowerFirstChar(nameString);
        }

        public static string WithLowerFirstChar(this NameString nameString)
        {
            return char.ToLower(nameString.Value[0]) + nameString.Value.Substring(1);
        }

        public static string WithCapitalFirstChar(this NameString nameString)
        {
            return char.ToUpper(nameString.Value[0]) + nameString.Value.Substring(1);
        }
    }
}
