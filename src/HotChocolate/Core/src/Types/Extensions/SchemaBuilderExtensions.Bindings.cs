using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder BindResolver<TResolver>(
            this ISchemaBuilder builder)
            where TResolver : class =>
            BindResolver<TResolver>(
                builder,
                BindingBehavior.Implicit,
                null);

        public static ISchemaBuilder BindResolver<TResolver>(
            this ISchemaBuilder builder,
            Action<IBindResolver<TResolver>> configure)
            where TResolver : class =>
            BindResolver<TResolver>(
                builder,
                BindingBehavior.Implicit,
                configure);

        public static ISchemaBuilder BindResolver<TResolver>(
            this ISchemaBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindResolver<TResolver>> configure)
            where TResolver : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (bindingBehavior == BindingBehavior.Explicit
                && configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            IResolverTypeBindingBuilder bindingBuilder =
                ResolverTypeBindingBuilder.New()
                    .SetFieldBinding(bindingBehavior)
                    .SetResolverType(typeof(TResolver));

            if (configure != null)
            {
                configure(new BindResolver<TResolver>(bindingBuilder));
            }

            return builder.AddBinding(bindingBuilder.Create());
        }

        public static ISchemaBuilder BindComplexType<T>(
            this ISchemaBuilder builder)
            where T : class =>
            BindComplexType<T>(
                builder,
                BindingBehavior.Implicit,
                null);

        public static ISchemaBuilder BindComplexType<T>(
            this ISchemaBuilder builder,
            Action<IBindType<T>> configure)
            where T : class =>
            BindComplexType<T>(
                builder,
                BindingBehavior.Implicit,
                configure);

        public static ISchemaBuilder BindComplexType<T>(
            this ISchemaBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindType<T>> configure)
            where T : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (bindingBehavior == BindingBehavior.Explicit
                && configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            IComplexTypeBindingBuilder bindingBuilder =
                ComplexTypeBindingBuilder.New()
                    .SetFieldBinding(bindingBehavior)
                    .SetType(typeof(T));

            if (configure != null)
            {
                configure(new BindType<T>(bindingBuilder));
            }

            return builder.AddBinding(bindingBuilder.Create());
        }
    }
}
