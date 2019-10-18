using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private DescriptorContext(
            IReadOnlySchemaOptions options,
            INamingConventions naming,
            ITypeInspector inspector,
            IReadOnlyDictionary<Type, IConvention> conventions)
        {
            Options = options;
            Naming = naming;
            Inspector = inspector;
            _conventions = conventions;
        }

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming { get; }

        public ITypeInspector Inspector { get; }

        public IReadOnlyDictionary<Type, IConvention> _conventions;

        public static DescriptorContext Create(
            IReadOnlySchemaOptions options,
            IServiceProvider services,
            IReadOnlyDictionary<Type, IConvention> conventions)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
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

            return new DescriptorContext(options, naming, inspector, conventions);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new SchemaOptions(),
                new DefaultNamingConventions(),
                new DefaultTypeInspector(),
                new Dictionary<Type, IConvention>());
        }

        public T GetConvention<T>() where T : IConvention
        {
            TryGetConvention<T>(out T convention);
            return convention;
        }

        public bool TryGetConvention<T>(out T convention) where T : IConvention
        {
            if (_conventions.TryGetValue(typeof(T), out IConvention noConvention)
                && noConvention is T conventionOfT)
            {
                convention = conventionOfT;
                return true;
            }
            convention = default;
            return false;
        }
    }
}
