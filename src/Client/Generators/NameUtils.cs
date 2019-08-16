using System;

namespace StrawberryShake.Generators
{
    internal static class NameUtils
    {
        public static string GetInterfaceName(string typeName)
        {
            if (typeName.Length > 1
                && char.IsUpper(typeName[0])
                && char.IsUpper(typeName[1])
                && typeName[0] == 'I')
            {
                return GetPropertyName(typeName);
            }

            return 'I' + GetPropertyName(typeName);
        }

        public static string GetPropertyName(string fieldName)
        {
            int buffered = 0;
            Span<char> amended = stackalloc char[fieldName.Length];

            for (int i = 0; i < fieldName.Length; i++)
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

            return new string(amended.Slice(0, buffered));
        }
    }
}
