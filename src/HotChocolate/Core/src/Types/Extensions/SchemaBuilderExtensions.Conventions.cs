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
            IConvention concreteConvention)
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

            return builder.AddConvention(convention, (s) => concreteConvention);
        }

        public static ISchemaBuilder AddConvention(
            this ISchemaBuilder builder,
            Type convention,
            Type concreteConvention)
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
                s => (IConvention)s.GetService(concreteConvention));
        }

        public static ISchemaBuilder AddConvention<T>(
            this ISchemaBuilder builder, IConvention convention)
            where T : IConvention =>
            builder.AddConvention(typeof(T), convention);

        public static ISchemaBuilder AddConvention<TConvetion, TConcreteConvention>(
            this ISchemaBuilder builder)
            where TConvetion : IConvention
            where TConcreteConvention : IConvention =>
            builder.AddConvention(typeof(TConvetion), typeof(TConcreteConvention));
    }
}
