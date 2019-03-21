using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        public SchemaConfiguration()
        {
            Builder.SetOptions(Options);
        }

        public ISchemaOptions Options { get; set; } = new SchemaOptions();

        public ISchemaBuilder Builder { get; } = SchemaBuilder.New();

        public void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            Builder.AddServices(serviceProvider);
        }

        public IMiddlewareConfiguration Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            Builder.Use(middleware);
            return this;
        }
    }
}
