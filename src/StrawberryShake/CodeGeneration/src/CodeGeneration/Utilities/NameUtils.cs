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
            Path current = path;

            while (current != null)
            {
                builder.Insert(0, GetPropertyName(current.Name));
                current = current.Parent;
            }

            return builder.ToString();
        }

#if NETCOREAPP3_0 || NETCOREAPP2_2
        public static string GetPropertyName(string fieldName)
#else
        public unsafe static string GetPropertyName(string fieldName)
#endif
        {
            var buffered = 0;
            Span<char> amended = stackalloc char[fieldName.Length];

            for (var i = 0; i < fieldName.Length; i++)
            {
                if (i == 0 && char.IsLetter(fieldName[i]))
                {
                    amended[buffered++] = char.ToUpper(fieldName[i], CultureInfo.InvariantCulture);
                }
                else if (fieldName[i] == '_')
                {
                    if (i + 1 < fieldName.Length
                        && char.IsLetter(fieldName[i + 1]))
                    {
                        amended[buffered++] =
                            char.ToUpper(fieldName[++i], CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    amended[buffered++] = fieldName[i];
                }
            }

#if NETCOREAPP3_0 || NETCOREAPP2_2
            return new string(amended.Slice(0, buffered));
#else

            amended = amended.Slice(0, buffered);
            fixed (char* charPtr = amended)
            {
                return new string(charPtr, 0, amended.Length);
            }
#endif
        }

        public static string GetFieldName(params string[] s)
        {
            return GetFieldName(string.Join(string.Empty, s));
        }

        public static string GetFieldName(string fieldName)
        {
            return "_" + GetParameterName(fieldName);
        }

#if NETCOREAPP3_1 || NETCOREAPP2_1
        public static string GetParameterName(string parameterName)
#else
        public unsafe static string GetParameterName(string parameterName)
#endif
        {
            var buffered = 0;
            var first = true;
            Span<char> amended = stackalloc char[parameterName.Length];

            for (var i = 0; i < parameterName.Length; i++)
            {
                if (i == 0 && char.IsLetter(parameterName[i]))
                {
                    amended[buffered++] = char.ToLower(parameterName[i], CultureInfo.InvariantCulture);
                    first = false;
                }
                else if (parameterName[i] == '_')
                {
                    if (i + 1 < parameterName.Length
                        && char.IsLetter(parameterName[i + 1]))
                    {
                        amended[buffered++] = first
                            ? char.ToLower(parameterName[++i], CultureInfo.InvariantCulture)
                            : char.ToUpper(parameterName[++i], CultureInfo.InvariantCulture);
                        first = false;
                    }
                }
                else
                {
                    amended[buffered++] = parameterName[i];
                }
            }

#if NETCOREAPP3_1 || NETCOREAPP2_1
            return new string(amended.Slice(0, buffered));
#else

            amended = amended.Slice(0, buffered);
            fixed (char* charPtr = amended)
            {
                return new string(charPtr, 0, amended.Length);
            }
#endif
        }
    }
}
