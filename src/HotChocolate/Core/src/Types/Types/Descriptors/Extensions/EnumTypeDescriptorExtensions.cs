using System;

namespace HotChocolate.Types
{
    public static class EnumTypeDescriptorExtensions
    {
        public static IEnumTypeDescriptor<T> Ignore<T>(
            this IEnumTypeDescriptor<T> descriptor,
            T value)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            descriptor.Value(value).Ignore();
            return descriptor;
        }

        public static IEnumTypeDescriptor Ignore<T>(
            this IEnumTypeDescriptor descriptor,
            T value)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            descriptor.Value(value).Ignore();
            return descriptor;
        }
    }
}
