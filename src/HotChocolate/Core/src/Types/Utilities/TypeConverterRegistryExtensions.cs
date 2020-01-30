using System;

namespace HotChocolate.Utilities
{
    public static class TypeConverterRegistryExtensions
    {
        public static void Register(
            this ITypeConverterRegistry registry,
            ITypeConverter converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            registry.Register(converter.From, converter.To, converter.Convert);
        }
    }
}
