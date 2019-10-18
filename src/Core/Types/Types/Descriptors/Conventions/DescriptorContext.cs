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
            IDictionary<Type, IConvention> conventions)
        {
            Options = options;
            Naming = naming;
            Inspector = inspector;
            _conventions = conventions;
        }

        public IReadOnlySchemaOptions Options { get; }

        public INamingConventions Naming { get; }

        public ITypeInspector Inspector { get; }

        private readonly IDictionary<Type, IConvention> _conventions;

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



            var conventionList =
                (IEnumerable<IConvention>)services.GetService(
                    typeof(IEnumerable<IConvention>)
                    ) ?? new IConvention[] { };

            var conventions =
                conventionList.ToDictionary(x => x.GetType());

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
            if (_conventions.TryGetValue(typeof(T), out IConvention convetion)
                && convetion is T conventionOfT)
            {
                return conventionOfT;
            }
            throw new
                NotImplementedException($"The convetion of type ${typeof(T)} is not registered");
        }
    }
}
