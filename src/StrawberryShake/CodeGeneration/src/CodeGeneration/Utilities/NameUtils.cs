using System;
using System.Globalization;
using System.Text;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class NameUtils
    {
        public static string GetInterfaceName(string typeName)
        {
            return 'I' + GetPropertyName(typeName);
        }

        public static string GetClassName(params string[] s)
        {
            return GetClassName(string.Join(string.Empty, s));
        }

        public static string GetClassName(string typeName)
        {
            return GetPropertyName(typeName);
        }

        public static string GetPathName(Path path)
        {
            var builder = new StringBuilder();
            Path? current = path;

            while (current != null)
            {
                if (current is NamePathSegment nameSegment)
                {
                    builder.Insert(0, GetPropertyName(nameSegment.Name));
                }

                current = current.Parent;
            }

            return builder.ToString();
        }

        public static string GetPropertyName(string fieldName)
        {
            var value = new StringBuilder();
            ReadOnlySpan<char> fieldNameSpan = fieldName.AsSpan();

            for (var i = 0; i < fieldName.Length; i++)
            {
                if (i == 0 && char.IsLetter(fieldName[i]))
                {

                    value.Append(char.ToUpper(fieldName[i], CultureInfo.InvariantCulture));
                }
                else
                {
                    value.Append(fieldName[i]);
                }
            }

            return value.ToString();
        }

        public static string GetEnumValue(string enumValue)
        {
            var value = new StringBuilder();
            ReadOnlySpan<char> enumValueSpan = enumValue.AsSpan();

            var upper = true;

            for (var i = 0; i < enumValueSpan.Length; i++)
            {
                if (enumValueSpan[i] == '_')
                {
                    upper = true;
                }
                else if (upper)
                {
                    upper = false;
                    value.Append(char.ToUpperInvariant(enumValueSpan[i]));
                }
                else
                {
                    value.Append(char.ToLowerInvariant(enumValueSpan[i]));
                }
            }

            return value.ToString();
        }

        public static string GetFieldName(params string[] s)
        {
            return GetFieldName(string.Join(string.Empty, s));
        }

        public static string GetFieldName(string fieldName)
        {
            return "_" + GetParameterName(fieldName);
        }

        public static string GetParameterName(string parameterName)
        {
            var value = new StringBuilder();
            var first = true;

            for (var i = 0; i < parameterName.Length; i++)
            {
                if (i == 0 && char.IsLetter(parameterName[i]))
                {
                    value.Append(char.ToLower(parameterName[i], CultureInfo.InvariantCulture));
                    first = false;
                }
                else if (parameterName[i] == '_')
                {
                    if (i + 1 < parameterName.Length
                        && char.IsLetter(parameterName[i + 1]))
                    {
                        value.Append(first
                            ? char.ToLower(parameterName[++i], CultureInfo.InvariantCulture)
                            : char.ToUpper(parameterName[++i], CultureInfo.InvariantCulture));
                        first = false;
                    }
                }
                else
                {
                    value.Append(parameterName[i]);
                }
            }

            return value.ToString();
        }
    }
}
