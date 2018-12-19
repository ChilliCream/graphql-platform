using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class SchemaConfigurationExtensions
    {
        public static IBindResolverDelegate BindResolver(
            this ISchemaFirstConfiguration schemaConfiguration,
            Func<IResolverContext, object> resolver)
        {
            return schemaConfiguration.BindResolver(
                ctx => Task.FromResult(resolver(ctx)));
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaFirstConfiguration schemaConfiguration,
            Func<object> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(
                ctx => Task.FromResult(resolver()));
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaFirstConfiguration schemaConfiguration,
            Func<Task<object>> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(ctx => resolver());
        }

        public static IBindResolverDelegate BindResolver(
            this ISchemaFirstConfiguration schemaConfiguration,
            Func<IResolverContext, CancellationToken, Task<object>> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return schemaConfiguration.BindResolver(
                ctx => resolver(ctx, ctx.RequestAborted));
        }

        public static IBindType<T> BindType<T>(
            this ISchemaFirstConfiguration configuration)
            where T : class
        {
            return configuration.BindType<T>(BindingBehavior.Implicit);
        }

        public static IBindResolver<TResolver> BindResolver<TResolver>(
            this ISchemaFirstConfiguration configuration)
            where TResolver : class
        {
            return configuration.BindResolver<TResolver>(
                BindingBehavior.Implicit);
        }
    }
}
