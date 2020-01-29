using System;
using System.Text;
using HotChocolate;

namespace StrawberryShake.Generators.Utilities
{
    internal static class NameUtils
    {
        public static string GetInterfaceName(string typeName)
        {
            return 'I' + GetPropertyName(typeName);
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
                    amended[buffered++] = char.ToUpper(fieldName[i]);
                }
                else if (fieldName[i] == '_')
                {
                    if (i + 1 < fieldName.Length
                        && char.IsLetter(fieldName[i + 1]))
                    {
                        amended[buffered++] = char.ToUpper(fieldName[++i]);
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

#if NETCOREAPP3_0 || NETCOREAPP2_2
        public static string GetFieldName(string fieldName)
#else
        public unsafe static string GetFieldName(string fieldName)
#endif
        {
            var buffered = 0;
            Span<char> amended = stackalloc char[fieldName.Length];

            for (var i = 0; i < fieldName.Length; i++)
            {
                if (i == 0 && char.IsLetter(fieldName[i]))
                {
                    amended[buffered++] = char.ToLower(fieldName[i]);
                }
                else if (fieldName[i] == '_')
                {
                    if (i + 1 < fieldName.Length
                        && char.IsLetter(fieldName[i + 1]))
                    {
                        amended[buffered++] = char.ToUpper(fieldName[++i]);
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
    }
}
