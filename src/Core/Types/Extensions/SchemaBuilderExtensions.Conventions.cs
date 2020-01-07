using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            IConvention concreteConvention) =>
                builder.AddNamedConvention(Convention.DefaultName,
                    convention, concreteConvention);

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention) =>
                builder.AddNamedConvention(Convention.DefaultName,
                    convention, concreteConvention);

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder, IConvention convention)
            where T : IConvention =>
                builder.AddNamedConvention<T>(Convention.DefaultName, convention);

        public static ISchemaBuilder AddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder)
            where TConvetion : IConvention
            where TConcreteConvention : TConvetion =>
                builder.AddNamedConvention<TConvetion,
                    TConcreteConvention>(Convention.DefaultName);

        public static ISchemaBuilder AddNamedConvention(
            this ISchemaBuilder builder,
            string name,
            Type convention,
            IConvention concreteConvention)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (concreteConvention == null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (!typeof(IConvention).IsAssignableFrom(convention))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Convention_NotSuppported,
                    nameof(convention));
            }

            builder.AddNamedConvention(name, convention, (s) => concreteConvention);

            return builder;
        }

        public static ISchemaBuilder AddNamedConvention(
            this ISchemaBuilder builder,
            string name,
            Type convention,
            Type concreteConvention)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (concreteConvention == null)
            {
                throw new ArgumentNullException(nameof(concreteConvention));
            }

            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
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

            builder.AddNamedConvention(name, convention,
                (s) => (IConvention)s.GetService(concreteConvention));

            return builder;
        }

        public static ISchemaBuilder AddNamedConvention<T>(
            this ISchemaBuilder builder,
            string name,
            IConvention convention)
            where T : IConvention
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            builder.AddNamedConvention(name, typeof(T), convention);
            return builder;
        }

        public static ISchemaBuilder AddNamedConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder,
            string name)
            where TConvetion : IConvention
            where TConcreteConvention : TConvetion
        {
            builder.AddNamedConvention(
                name,
                typeof(TConvetion),
                typeof(TConcreteConvention)
            );

            return builder;
        }
    }
}
