using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
        , ISchemaConfigurationExtension
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
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _builder.Use(middleware);
            return this;
        }

        public ISchemaConfigurationExtension Extend()
        {
            return this;
        }

        ISchemaConfiguration ISchemaConfigurationExtension.OnBeforeBuild(
            Action<ISchemaBuilder> build)
        {
            if (build is null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            build(_builder);
            return this;
        }
    }
}
