using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class ResolverRegistry
        : IResolverRegistry
    {
        private readonly FieldResolverBuilder _fieldResolverBuilder =
            new FieldResolverBuilder();
        private readonly Dictionary<FieldReference, FieldResolverDelegate> _resolvers =
            new Dictionary<FieldReference, FieldResolverDelegate>();
        private readonly Dictionary<FieldReference, ResolverBinding> _resolverBindings =
            new Dictionary<FieldReference, ResolverBinding>();
        private readonly Dictionary<FieldReference, FieldResolverDescriptor> _resolverDescriptors =
            new Dictionary<FieldReference, FieldResolverDescriptor>();

        public void RegisterResolver(ResolverBinding resolverBinding)
        {
            if (resolverBinding == null)
            {
                throw new ArgumentNullException(nameof(resolverBinding));
            }

            FieldReference fieldReference = new FieldReference(
                resolverBinding.TypeName, resolverBinding.FieldName);
            _resolverBindings[fieldReference] = resolverBinding;
        }

        public void RegisterResolver(FieldResolverDescriptor resolverDescriptor)
        {
            if (resolverDescriptor == null)
            {
                throw new ArgumentNullException(nameof(resolverDescriptor));
            }

            _registeredResolvers.Add(resolverDescriptor.Field);
            _resolverDescriptors.Add(resolverDescriptor);
        }

        public bool ContainsResolver(FieldReference fieldReference)
        {
            throw new NotImplementedException();
        }

        public FieldResolverDelegate GetResolver(string typeName, string fieldName)
        {
            FieldReference fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference, out FieldResolverDelegate resolver))
            {
                return resolver;
            }
            throw new ArgumentException(
                "No resolver was configured for `{typeName}.{fieldName}`.");
        }



        internal void BuildResolvers()
        {
            List<FieldResolver> fieldResolvers = new List<FieldResolver>();
            fieldResolvers.AddRange(CompileResolvers());

            foreach (FieldResolver resolver in fieldResolvers)
            {
                FieldReference fieldReference = new FieldReference(
                    resolver.TypeName, resolver.FieldName);
                _resolvers[fieldReference] = resolver.Resolver;
            }
        }

        private IEnumerable<FieldResolver> CompileResolvers()
        {
            List<FieldResolverDescriptor> resolverDescriptors = new List<FieldResolverDescriptor>();
            foreach (MemberResolverBinding binding in _resolverBindings
                .OfType<MemberResolverBinding>())
            {
                if (binding.FieldMember is PropertyInfo p)
                {
                    FieldReference fieldReference = new FieldReference(binding.TypeName, binding.FieldName);
                    resolverDescriptors.Add(FieldResolverDescriptor.CreateSourceProperty(fieldReference, p.ReflectedType, p));
                }
            }

            if (resolverDescriptors.Any())
            {
                return _fieldResolverBuilder.Build(resolverDescriptors);
            }
            return Enumerable.Empty<FieldResolver>();
        }
    }
}
