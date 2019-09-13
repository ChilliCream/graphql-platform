using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate
{
    public static class TypeConverterServiceCollectionExtensions
    {
        public static IServiceCollection AddTypeConverter<T>(
            this IServiceCollection serviceCollection)
            where T : class, ITypeConverter
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.TryAddSingleton<ITypeConversion>(sp =>
                new TypeConversion(sp.GetServices<ITypeConverter>()));
            return serviceCollection.AddSingleton<ITypeConverter, T>();
        }

        public static IServiceCollection AddTypeConverter<TFrom, TTo>(
            this IServiceCollection serviceCollection,
            ChangeType<TFrom, TTo> changeType)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.TryAddSingleton<ITypeConversion>(sp =>
                new TypeConversion(sp.GetServices<ITypeConverter>()));
            return serviceCollection.AddSingleton<ITypeConverter>(
                new DelegateTypeConverter<TFrom, TTo>(changeType));
        }
    }
}
