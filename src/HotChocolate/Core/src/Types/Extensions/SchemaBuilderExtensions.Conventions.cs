using System;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            CreateConvention conventionFactory,
            string scope = ConventionBase.DefaultScope)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            return builder.AddConvention(
                scope, convention, conventionFactory);
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            IConvention concreteConvention,
            string scope = ConventionBase.DefaultScope)
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
            string scope = ConventionBase.DefaultScope)
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
                s => (IConvention)s.GetService(concreteConvention),
                scope);
        }

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder,
            IConvention convention,
            string scope = ConventionBase.DefaultScope)
            where T : IConvention =>
            builder.AddConvention(typeof(T), convention, scope);

        public static ISchemaBuilder AddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder,
            string scope = ConventionBase.DefaultScope)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention =>
            builder.AddConvention(typeof(TConvetion), typeof(TConcreteConvention), scope);

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            CreateConvention conventionFactory,
            string scope = ConventionBase.DefaultScope)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            return builder.TryAddConvention(
                scope, convention, conventionFactory);
        }

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            IConvention concreteConvention,
            string scope = ConventionBase.DefaultScope)
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

            return builder.TryAddConvention(convention, (s) => concreteConvention, scope);
        }

        public static ISchemaBuilder TryAddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention,
            string scope = ConventionBase.DefaultScope)
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
                s => (IConvention)s.GetService(concreteConvention),
                scope);
        }

        public static ISchemaBuilder TryAddConvention<T>(
            this ISchemaBuilder builder,
            CreateConvention conventionFactory,
            string scope = ConventionBase.DefaultScope)
            where T : IConvention =>
            builder.TryAddConvention(typeof(T), conventionFactory, scope);

        public static ISchemaBuilder TryAddConvention<T>(
            this ISchemaBuilder builder,
            IConvention convention,
            string scope = ConventionBase.DefaultScope)
            where T : IConvention =>
            builder.TryAddConvention(typeof(T), convention, scope);

        public static ISchemaBuilder TryAddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder,
            string scope = ConventionBase.DefaultScope)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention =>
            builder.TryAddConvention(typeof(TConvetion), typeof(TConcreteConvention), scope);
    }
}
