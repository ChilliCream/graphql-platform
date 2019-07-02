using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate
{
    public static class TypeConverterServiceCollectionExtensions
    {
        public static IServiceCollection AddTypeConverter<T>(
            IServiceCollection serviceCollection)
            where T : class, ITypeConverter
        {
            serviceCollection.TryAddSingleton<ITypeConversion>(sp =>
                new TypeConversion(sp.GetServices<ITypeConverter>()));
            return serviceCollection.AddSingleton<ITypeConverter, T>();
        }

        public static IServiceCollection AddTypeConverter<TFrom, TTo>(
            IServiceCollection serviceCollection,
            ChangeType<TFrom, TTo> changeType)
        {
            serviceCollection.TryAddSingleton<ITypeConversion>(sp =>
                new TypeConversion(sp.GetServices<ITypeConverter>()));
            return serviceCollection.AddSingleton<ITypeConverter>(
                new DelegateTypeConverter<TFrom, TTo>(changeType));
        }
    }
}
