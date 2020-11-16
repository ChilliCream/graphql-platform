using System;
using System.Collections.Generic;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder BindResolver<TResolver>(
            this ISchemaBuilder builder,
            Action<IBindResolver<TResolver>>? configure = default)
            where TResolver : class =>
            BindResolver(
                builder,
                BindingBehavior.Implicit,
                configure);

        public static ISchemaBuilder BindResolver<TResolver>(
            this ISchemaBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindResolver<TResolver>>? configure)
            where TResolver : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (bindingBehavior == BindingBehavior.Explicit
                && configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            IResolverTypeBindingBuilder bindingBuilder =
                ResolverTypeBindingBuilder.New()
                    .SetFieldBinding(bindingBehavior)
                    .SetResolverType(typeof(TResolver));

            configure?.Invoke(new BindResolver<TResolver>(bindingBuilder));
            return builder.AddBinding(bindingBuilder.Create());
        }

        public static ISchemaBuilder BindComplexType<T>(
            this ISchemaBuilder builder,
            Action<IBindType<T>>? configure = default)
            where T : class =>
            BindComplexType(
                builder,
                BindingBehavior.Implicit,
                configure);

        public static ISchemaBuilder BindComplexType<T>(
            this ISchemaBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindType<T>>? configure)
            where T : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (bindingBehavior == BindingBehavior.Explicit
                && configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            IComplexTypeBindingBuilder bindingBuilder =
                ComplexTypeBindingBuilder.New()
                    .SetFieldBinding(bindingBehavior)
                    .SetType(typeof(T));

            configure?.Invoke(new BindType<T>(bindingBuilder));
            return builder.AddBinding(bindingBuilder.Create());
        }

        public static ISchemaBuilder BindEnumType<T>(
            this ISchemaBuilder builder,
            Action<IEnumTypeBindingDescriptor>? configure = default) =>
            BindEnumType(builder, typeof(T), configure);

        public static ISchemaBuilder BindEnumType(
            this ISchemaBuilder builder,
            Type runtimeType,
            Action<IEnumTypeBindingDescriptor>? configure = default)
        {
            configure ??= _ => { };

            return builder
                .SetContextData(
                    SchemaFirstContextData.EnumTypeConfigs,
                    o =>
                    {
                        if (o is List<EnumTypeBindingConfiguration> configs)
                        {
                            configs.Add(new EnumTypeBindingConfiguration(runtimeType, configure));
                            return o;
                        }

                        return new List<EnumTypeBindingConfiguration>
                        {
                            new EnumTypeBindingConfiguration(runtimeType, configure)
                        };
                    })
                .TryAddTypeInterceptor<SchemaFirstTypeInterceptor>()
                .TryAddSchemaInterceptor<SchemaFirstSchemaInterceptor>();
        }
    }
}
