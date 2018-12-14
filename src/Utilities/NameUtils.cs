using System;
using System.Text;

namespace HotChocolate.Utilities
{
    internal static class NameUtils
    {
        public static bool IsValidName(string name)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }

            if (name[0].IsLetterOrUnderscore())
            {
                if (name.Length > 1)
                {
                    for (int i = 1; i < name.Length; i++)
                    {
                        if (!name[i].IsLetterOrDigitOrUnderscore())
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static string RemoveInvalidCharacters(string name)
        {
            if (name == null || name.Length == 0)
            {
                return name;
            }

            char[] namearray = name.ToCharArray();

            if (!namearray[0].IsLetterOrUnderscore())
            {
                namearray[0] = '_';
            }

            if (namearray.Length > 1)
            {
                for (int i = 1; i < namearray.Length; i++)
                {
                    if (!namearray[i].IsLetterOrDigitOrUnderscore())
                    {
                        namearray[i] = '_';
                    }
                }
            }

            return new string(namearray);
        }
    }
}
