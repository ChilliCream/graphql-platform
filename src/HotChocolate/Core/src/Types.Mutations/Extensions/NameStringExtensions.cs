using System.Text;

namespace HotChocolate.Types
{
    internal static class NameStringExtensions
    {
        public static NameString ToTypeName(
            this NameString name,
            string parameterName = "",
            string suffix = "")
        {
            if (!name.HasValue || name is { Value: { Length: <1 } })
            {
                return string.Empty;
            }

            StringBuilder typeNameBuilder = new StringBuilder()
                .Append(char.ToUpper(name.Value[0]))
                .Append(name.Value.Substring(1));

            if (!string.IsNullOrWhiteSpace(parameterName))
            {
                typeNameBuilder = typeNameBuilder
                    .Append(char.ToUpper(parameterName[0]))
                    .Append(parameterName.Substring(1));
            }

            var typeName = typeNameBuilder.ToString();

            if (typeName.EndsWith(suffix, StringComparison.InvariantCulture))
            {
                return typeName;
            }

            return $"{typeName}{suffix}";
        }

        public static string ToFieldName(this string name)
        {
            if (name is { Length: <1 })
            {
                return "";
            }

            return new StringBuilder()
                .Append(char.ToLower(name[0]))
                .Append(name.Substring(1))
                .ToString();
        }
    }
}
