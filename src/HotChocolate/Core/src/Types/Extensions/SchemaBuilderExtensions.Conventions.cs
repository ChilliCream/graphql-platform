using System;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder,
            Type type,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddConvention(typeof(T), type, scope);
        }

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder,
            CreateConvention conventionFactory,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddConvention(typeof(T), conventionFactory, scope);
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            CreateConvention conventionFactory,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            return builder.AddConvention(convention, conventionFactory, scope);
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            IConvention concreteConvention,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (concreteConvention is null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (!typeof(IConvention).IsAssignableFrom(convention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.AddConvention(convention, (s) => concreteConvention, scope);
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (concreteConvention is null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (!typeof(IConvention).IsAssignableFrom(convention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.AddConvention(
                convention,
                s =>
                {
                    if (s.TryGetOrCreateService<IConvention>(
                        concreteConvention,
                        out IConvention convention))
                    {
                        return convention;
                    }

                    throw Convention_UnableToCreateConvention(concreteConvention);
                },
                scope);
        }

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder,
            IConvention convention,
            string? scope = null)
            where T : IConvention =>
            builder.AddConvention(typeof(T), convention, scope);

        public static ISchemaBuilder AddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder,
            string? scope = null)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention =>
            builder.AddConvention(typeof(TConvetion), typeof(TConcreteConvention), scope);

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            CreateConvention conventionFactory,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            return builder.TryAddConvention(convention, conventionFactory, scope);
        }

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            IConvention concreteConvention,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (concreteConvention is null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (!typeof(IConvention).IsAssignableFrom(convention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.TryAddConvention(convention, s => concreteConvention, scope);
        }

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (concreteConvention is null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (!typeof(IConvention).IsAssignableFrom(convention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.TryAddConvention(
                convention,
                s =>
                {
                    if (s.TryGetOrCreateService(concreteConvention, out IConvention? c))
                    {
                        return c;
                    }

                    throw Convention_UnableToCreateConvention(concreteConvention);
                },
                scope);
        }

        public static ISchemaBuilder TryAddConvention<T>(
            this ISchemaBuilder builder,
            CreateConvention conventionFactory,
            string? scope = null)
            where T : IConvention =>
            builder.TryAddConvention(typeof(T), conventionFactory, scope);

        public static ISchemaBuilder TryAddConvention<T>(
            this ISchemaBuilder builder,
            Type type,
            string? scope = null)
            where T : IConvention =>
            builder.TryAddConvention(typeof(T), type, scope);

        public static ISchemaBuilder TryAddConvention<T>(
            this ISchemaBuilder builder,
            IConvention convention,
            string? scope = null)
            where T : IConvention =>
            builder.TryAddConvention(typeof(T), convention, scope);

        public static ISchemaBuilder TryAddConvention<TConvention, TConcreteConvention>(
            this ISchemaBuilder builder,
            string? scope = null)
            where TConvention : IConvention
            where TConcreteConvention : class, TConvention =>
            builder.TryAddConvention(typeof(TConvention), typeof(TConcreteConvention), scope);
    }
}
