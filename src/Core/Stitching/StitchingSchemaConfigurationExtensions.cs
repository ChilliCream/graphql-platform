using System.Collections.Generic;
using HotChocolate.Stitching;

namespace HotChocolate
{
    public static class StitchingSchemaConfigurationExtensions
    {
        public static ISchemaConfiguration UseStitching(
            this ISchemaConfiguration configuration)
        {
            configuration.RegisterDirective<DelegateDirectiveType>();
            configuration.RegisterDirective<SchemaDirectiveType>();

            configuration.Use(next => context =>
            {
                if (context.Parent<object>() is IDictionary<string, object> d)
                {
                    context.Result = d[context.Field.Name];
                }

                return next(context);
            });

            return configuration;
        }
    }
}
