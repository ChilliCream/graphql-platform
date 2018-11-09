using System;

namespace HotChocolate.Language
{
    public static class ValidationUtils
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

    }
}
