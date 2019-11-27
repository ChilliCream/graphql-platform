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
            IConvention concreteConvention)
        {
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

            builder.AddConvention(convention, (s) => concreteConvention);

            return builder;
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention)
        {
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

            builder.AddConvention(convention, (s) => (IConvention)s.GetService(concreteConvention));

            return builder;
        }

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder, IConvention convention)
            where T : IConvention
        {
            builder.AddConvention(typeof(T), convention);
            return builder;
        }

        public static ISchemaBuilder AddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention
        {
            builder.AddConvention(
                typeof(TConvetion),
                typeof(TConcreteConvention)
            );

            return builder;
        }
    }
}
