using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly SchemaBuilder _builder = SchemaBuilder.New();


        public ISchemaOptions Options { get; set; } = new SchemaOptions();

        public SchemaBuilder CreateBuilder()
        {
            foreach (IBindingBuilder bindingBuilder in _bindingBuilders)
            {
                if (bindingBuilder.IsComplete())
                {
                    _builder.AddBinding(bindingBuilder.Create());
                }
            }

            _builder.SetOptions(Options);

            return _builder;
        }

        public ISchemaConfiguration RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            _builder.AddServices(serviceProvider);
            return this;
        }

        public ISchemaConfiguration Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _builder.Use(middleware);
            return this;
        }

        public ISchemaConfiguration Extend(Action<ISchemaBuilder> build)
        {
            build(_builder);
            return this;
        }
    }
}
