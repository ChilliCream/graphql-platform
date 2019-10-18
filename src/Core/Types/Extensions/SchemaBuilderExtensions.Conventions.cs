using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
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
