using System;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public static class BindableListExtensions
    {
        public static bool IsImplicitBinding<T>(this IBindableList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            return list.BindingBehavior == BindingBehavior.Implicit;
        }
    }
}
