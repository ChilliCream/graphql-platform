using System;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private DescriptorContext(
            IReadOnlySchemaOptions options,
            INamingConventions naming,
            ITypeInspector inspector)
        {
            Options = options;
            Naming = naming;
            Inspector = inspector;
        }

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming { get; }

        public ITypeInspector Inspector { get; }

        public static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services)
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

            return new DescriptorContext(options, naming, inspector);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new SchemaOptions(),
                new DefaultNamingConventions(),
                new DefaultTypeInspector());
        }
    }
}
