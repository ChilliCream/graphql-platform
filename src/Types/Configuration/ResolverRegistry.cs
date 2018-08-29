using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal class ResolverRegistry
        : IResolverRegistry
    {
        private readonly FieldResolverBuilder _resolverBuilder =
            new FieldResolverBuilder();
        private readonly Dictionary<FieldReference, FieldResolverDelegate> _resolvers =
            new Dictionary<FieldReference, FieldResolverDelegate>();
        private readonly Dictionary<FieldReference, IFieldReference> _resolverBindings =
            new Dictionary<FieldReference, IFieldReference>();
        private readonly Dictionary<FieldReference, IFieldResolverDescriptor> _resolverDescriptors =
            new Dictionary<FieldReference, IFieldResolverDescriptor>();

        public void RegisterResolver(IFieldReference resolverBinding)
        {
            if (resolverBinding == null)
            {
                throw new ArgumentNullException(nameof(resolverBinding));
            }

            var fieldReference = new FieldReference(
                resolverBinding.TypeName, resolverBinding.FieldName);
            _resolverBindings[fieldReference] = resolverBinding;
        }

        public void RegisterResolver(
            IFieldResolverDescriptor resolverDescriptor)
        {
            if (resolverDescriptor == null)
            {
                throw new ArgumentNullException(nameof(resolverDescriptor));
            }

            _resolverDescriptors[resolverDescriptor.Field.ToFieldReference()] =
                resolverDescriptor;
        }

        public bool ContainsResolver(FieldReference fieldReference)
        {
            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            return _resolverDescriptors.ContainsKey(fieldReference)
                || _resolverBindings.ContainsKey(fieldReference);
        }

        public FieldResolverDelegate GetResolver(string typeName, string fieldName)
        {
            var fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference, out FieldResolverDelegate resolver))
            {
                return resolver;
            }
            return null;
        }

        internal void BuildResolvers()
        {
            var fieldResolvers = new List<FieldResolver>();
            fieldResolvers.AddRange(CompileResolvers());
            fieldResolvers.AddRange(_resolverBindings.Values
                .OfType<FieldResolver>());

            foreach (FieldResolver resolver in fieldResolvers)
            {
                var fieldReference = new FieldReference(
                    resolver.TypeName, resolver.FieldName);
                _resolvers[fieldReference] = resolver.Resolver;
            }
        }

        private IEnumerable<FieldResolver> CompileResolvers()
        {
            var resolverDescriptors = new List<IFieldResolverDescriptor>();

            foreach (FieldMember binding in _resolverBindings.Values
                .OfType<FieldMember>())
            {
                resolverDescriptors.Add(new SourceResolverDescriptor(binding));
            }

            resolverDescriptors.AddRange(_resolverDescriptors.Values);

            if (resolverDescriptors.Count > 0)
            {
                return _resolverBuilder.Build(resolverDescriptors);
            }

            return Enumerable.Empty<FieldResolver>();
        }
    }
}
