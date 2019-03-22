using System;
using System.Collections.Generic;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly ISchemaBuilder _builder = SchemaBuilder.New();

        public SchemaConfiguration()
        {
            _builder.SetOptions(Options);
        }

        public ISchemaOptions Options { get; set; } = new SchemaOptions();

        public ISchemaBuilder CreateBuilder()
        {
            foreach (IBindingBuilder bindingBuilder in _bindingBuilders)
            {
                if (bindingBuilder.IsComplete())
                {
                    _builder.AddBinding(bindingBuilder.Create());
                }
            }
            return _builder;
        }

        public void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            _builder.AddServices(serviceProvider);
        }

        public IMiddlewareConfiguration Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _builder.Use(middleware);
            return this;
        }
    }
}
