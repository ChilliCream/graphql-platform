using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddConvention(
            this IRequestExecutorBuilder builder,
            Type convention,
            CreateConvention factory,
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

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.ConfigureSchema(b => b.AddConvention(convention, factory, scope));
        }

        public static IRequestExecutorBuilder TryAddConvention(
            this IRequestExecutorBuilder builder,
            Type convention,
            CreateConvention factory,
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

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.ConfigureSchema(b => b.TryAddConvention(convention, factory, scope));
        }

        public static IRequestExecutorBuilder AddConvention<T>(
            this IRequestExecutorBuilder builder,
            Type type,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddConvention(typeof(T), type, scope));
        }

        public static IRequestExecutorBuilder AddConvention<T>(
            this IRequestExecutorBuilder builder,
            CreateConvention conventionFactory,
            string? scope = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(
                b => b.AddConvention(typeof(T), conventionFactory, scope));
        }

        public static IRequestExecutorBuilder AddConvention(
            this IRequestExecutorBuilder builder,
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
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.ConfigureSchema(
                b => b.AddConvention(convention, (s) => concreteConvention, scope));
        }

        public static IRequestExecutorBuilder AddConvention(
            this IRequestExecutorBuilder builder,
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
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
            {
                throw new ArgumentException(
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.ConfigureSchema(
                b => b.AddConvention(
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
                    scope));
        }

        public static IRequestExecutorBuilder AddConvention<T>(
            this IRequestExecutorBuilder builder,
            IConvention convention,
            string? scope = null)
            where T : IConvention =>
            builder.ConfigureSchema(b => b.AddConvention(typeof(T), convention, scope));

        public static IRequestExecutorBuilder AddConvention<TConvetion, TConcreteConvention>(
            this IRequestExecutorBuilder builder,
            string? scope = null)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention =>
            builder.ConfigureSchema(
                b => b.AddConvention(typeof(TConvetion), typeof(TConcreteConvention), scope));

        public static IRequestExecutorBuilder TryAddConvention(
            this IRequestExecutorBuilder builder,
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
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.ConfigureSchema(
                b => b.TryAddConvention(convention, s => concreteConvention, scope));
        }

        public static IRequestExecutorBuilder TryAddConvention(
            this IRequestExecutorBuilder builder,
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
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
            {
                throw new ArgumentException(
                    Resources.RequestExecutorBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            return builder.ConfigureSchema(
                b => b.TryAddConvention(
                    convention,
                    s =>
                    {
                        if (s.TryGetOrCreateService(concreteConvention, out IConvention? c))
                        {
                            return c;
                        }

                        throw Convention_UnableToCreateConvention(concreteConvention);
                    },
                    scope));
        }

        public static IRequestExecutorBuilder TryAddConvention<T>(
            this IRequestExecutorBuilder builder,
            CreateConvention conventionFactory,
            string? scope = null)
            where T : IConvention =>
            builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), conventionFactory, scope));

        public static IRequestExecutorBuilder TryAddConvention<T>(
            this IRequestExecutorBuilder builder,
            Type type,
            string? scope = null)
            where T : IConvention =>
            builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), type, scope));

        public static IRequestExecutorBuilder TryAddConvention<T>(
            this IRequestExecutorBuilder builder,
            IConvention convention,
            string? scope = null)
            where T : IConvention =>
            builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), convention, scope));

        public static IRequestExecutorBuilder TryAddConvention<TConvention, TConcreteConvention>(
            this IRequestExecutorBuilder builder,
            string? scope = null)
            where TConvention : IConvention
            where TConcreteConvention : class, TConvention =>
            builder.ConfigureSchema(
                b => b.TryAddConvention(typeof(TConvention), typeof(TConcreteConvention), scope));
    }
}
