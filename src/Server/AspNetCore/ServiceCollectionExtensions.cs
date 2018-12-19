using System;
using Microsoft.Extensions.DependencyInjection;

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return serviceCollection
                .AddSingleton(schema)
                .AddSingleton(schema.MakeExecutable());
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory)
        {
            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            return serviceCollection
                .AddSingleton(schemaFactory)
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable());
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable());
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(
                    schemaSource, c =>
                    {
                        c.RegisterServiceProvider(s);
                        configure(c);
                    }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable());
        }
    }
}
