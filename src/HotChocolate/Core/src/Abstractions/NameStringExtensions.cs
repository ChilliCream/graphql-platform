using System;
using HotChocolate.Properties;

namespace HotChocolate
{
    public static class NameStringExtensions
    {
        public static NameString EnsureNotEmpty(
            in this NameString name,
            string argumentName)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    AbstractionResources.Name_MustNotBeEmpty,
                    argumentName);
            }

            return name;
        }
    }
}
