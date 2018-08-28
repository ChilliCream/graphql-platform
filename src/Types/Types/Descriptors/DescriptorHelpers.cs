using System;

namespace HotChocolate.Types
{
    internal class DescriptorHelpers
    {
        internal static T ExecuteFactory<T>(
            Func<T> descriptionFactory)
        {
            if (descriptionFactory == null)
            {
                throw new ArgumentNullException(nameof(descriptionFactory));
            }

            return descriptionFactory();
        }
    }
}
