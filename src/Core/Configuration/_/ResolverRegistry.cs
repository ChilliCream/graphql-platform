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
        private readonly FieldResolverBuilder _resolverBuilder =
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

            _resolverDescriptors[resolverDescriptor.Field] = resolverDescriptor;
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
            FieldReference fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference, out FieldResolverDelegate resolver))
            {
                return resolver;
            }
            return null;
        }

        internal void BuildResolvers()
        {
            List<FieldResolver> fieldResolvers = new List<FieldResolver>();
            fieldResolvers.AddRange(CompileResolvers());
            fieldResolvers.AddRange(_resolverBindings.Values
                .OfType<DelegateResolverBinding>()
                .Select(CreateDelegateResolver));

            foreach (FieldResolver resolver in fieldResolvers)
            {
                FieldReference fieldReference = new FieldReference(
                    resolver.TypeName, resolver.FieldName);
                _resolvers[fieldReference] = resolver.Resolver;
            }
        }

        private IEnumerable<FieldResolver> CompileResolvers()
        {
            List<FieldResolverDescriptor> resolverDescriptors =
                new List<FieldResolverDescriptor>();

            foreach (MemberResolverBinding binding in _resolverBindings.Values
                .OfType<MemberResolverBinding>())
            {
                if (binding.FieldMember is PropertyInfo p)
                {
                    FieldReference fieldReference = new FieldReference(
                        binding.TypeName, binding.FieldName);
                    resolverDescriptors.Add(FieldResolverDescriptor
                        .CreateSourceProperty(fieldReference, p.ReflectedType, p));
                }

                if (binding.FieldMember is MethodInfo m)
                {
                    FieldReference fieldReference = new FieldReference(binding.TypeName, binding.FieldName);
                    //resolverDescriptors.Add(FieldResolverDescriptor.CreateSourceMethod(fieldReference, m.ReflectedType, m));
                }
            }
            resolverDescriptors.AddRange(_resolverDescriptors.Values);

            if (resolverDescriptors.Any())
            {
                return _resolverBuilder.Build(resolverDescriptors);
            }
            return Enumerable.Empty<FieldResolver>();
        }

        private FieldResolver CreateDelegateResolver(DelegateResolverBinding binding)
        {
            return new FieldResolver(binding.TypeName, binding.FieldName, binding.FieldResolver);
        }
    }
}
