using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class SchemaConfigurationExtensions
    {
        public static IBindResolverDelegate BindResolver(
            this ISchemaConfiguration schemaConfiguration,
            Func<IResolverContext, object> resolver)
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            return schemaConfiguration.BindResolver(
                ctx => new ValueTask<object>(resolver(ctx)));
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaConfiguration schemaConfiguration,
            Func<object> resolver)
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(
                ctx => new ValueTask<object>(resolver()));
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaConfiguration schemaConfiguration,
            Func<Task<object>> resolver)
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(ctx => resolver());
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaConfiguration schemaConfiguration,
            Func<IResolverContext, CancellationToken, Task<object>> resolver)
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(
                ctx => resolver(ctx, ctx.RequestAborted));
        }

        public static IBindType<T> BindType<T>(
            this ISchemaConfiguration schemaConfiguration)
            where T : class
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            return schemaConfiguration.BindType<T>(BindingBehavior.Implicit);
        }

        public static IBindResolver<TResolver> BindResolver<TResolver>(
            this ISchemaConfiguration schemaConfiguration)
            where TResolver : class
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            return schemaConfiguration.BindResolver<TResolver>(
                BindingBehavior.Implicit);
        }

        public static ISchemaConfiguration RegisterExtendedScalarTypes(
            this ISchemaConfiguration schemaConfiguration)
        {
            if (schemaConfiguration is null)
            {
                throw new ArgumentNullException(nameof(schemaConfiguration));
            }

            schemaConfiguration.RegisterType(typeof(DecimalType));
            schemaConfiguration.RegisterType(typeof(ByteType));
            schemaConfiguration.RegisterType(typeof(ByteArrayType));
            schemaConfiguration.RegisterType(typeof(ShortType));
            schemaConfiguration.RegisterType(typeof(LongType));
            schemaConfiguration.RegisterType(typeof(DateTimeType));
            schemaConfiguration.RegisterType(typeof(DateType));
            schemaConfiguration.RegisterType(typeof(UuidType));
            schemaConfiguration.RegisterType(typeof(UrlType));

            return schemaConfiguration;
        }
    }
}
