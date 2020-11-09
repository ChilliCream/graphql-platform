using System;
namespace HotChocolate.Language
{
    /// <summary>
    /// Helper methods to handle GraphQL names.
    /// </summary>
    public static class NameUtils
    {
        /// <summary>
        /// Checks if the provided name is a valid GraphQL type or field name.
        /// </summary>
        /// <param name="name">
        /// The name that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c>, if the name is a valid GraphQL name;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidGraphQLName(string? name)
        {
            if (string.IsNullOrEmpty(name))
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

        /// <summary>
        /// Checks if the provided name is a valid GraphQL type or field name.
        /// </summary>
        /// <param name="name">
        /// The name that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c>, if the name is a valid GraphQL name;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidGraphQLName(ReadOnlySpan<byte> name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            if (name[0].IsLetterOrUnderscore())
            {
                if (name.Length > 1)
                {
                    for (var i = 1; i < name.Length; i++)
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

        /// <summary>
        /// Takes the provided name and replaces invalid
        /// charactes with an underscore.
        /// </summary>
        /// <param name="name">
        /// A name that shall be made a value GraphQL name.
        /// </param>
        /// <returns>Returns a valid GraphQL name.</returns>
        public static string? MakeValidGraphQLName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            char[] nameArray = name.ToCharArray();

            if (!nameArray[0].IsLetterOrUnderscore())
            {
                nameArray[0] = '_';
            }

            if (nameArray.Length > 1)
            {
                for (var i = 1; i < nameArray.Length; i++)
                {
                    if (!nameArray[i].IsLetterOrDigitOrUnderscore())
                    {
                        nameArray[i] = '_';
                    }
                }
            }

            return new string(nameArray);
        }
    }
}
