using System;
using HotChocolate.Configuration;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        internal event EventHandler SchemaResolved;

        private DescriptorContext(
            IServiceProvider services,
            IReadOnlySchemaOptions options,
            Func<ISchema> resolveSchema,
            INamingConventions naming,
            ITypeInspector inspector)
        {
            Services = services;
            Options = options;
            ResolveSchema = resolveSchema;
            Naming = naming;
            Inspector = inspector;
        }

        public IServiceProvider Services { get; }

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming { get; }

        public ITypeInspector Inspector { get; }

        internal Func<ISchema> ResolveSchema { get; }

        internal void TriggerSchemaResolved() => 
            SchemaResolved?.Invoke(this, EventArgs.Empty);

        public static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services,
            Func<ISchema> resolveSchema)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var naming =
                (INamingConventions)services.GetService(
                    typeof(INamingConventions));
            if (naming == null)
            {
                naming = options.UseXmlDocumentation
                    ? new DefaultNamingConventions(
                        new XmlDocumentationProvider(
                            new XmlDocumentationFileResolver()))
                    : new DefaultNamingConventions(
                        new NoopDocumentationProvider());
            }

            var inspector =
                (ITypeInspector)services.GetService(
                    typeof(ITypeInspector));
            if (inspector == null)
            {
                inspector = new DefaultTypeInspector();
            }

            return new DescriptorContext(services, options, resolveSchema, naming, inspector);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new EmptyServiceProvider(),
                new SchemaOptions(),
                () => throw new NotSupportedException(),
                new DefaultNamingConventions(),
                new DefaultTypeInspector());
        }
    }
}
