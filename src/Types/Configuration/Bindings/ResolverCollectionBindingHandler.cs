using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class ResolverCollectionBindingHandler
        : IResolverBindingHandler
    {
        private readonly FieldResolverDiscoverer _fieldResolverDiscoverer =
            new FieldResolverDiscoverer();
        private readonly ILookup<string, ResolverCollectionBindingInfo> _resolverBindings;

        public ResolverCollectionBindingHandler(
            IEnumerable<ResolverCollectionBindingInfo> resolverBindings)
        {
            if (resolverBindings == null)
            {
                throw new ArgumentNullException(nameof(resolverBindings));
            }
            _resolverBindings = resolverBindings.ToLookup(t => t.ObjectTypeName);
        }

        public void ApplyBinding(
            ISchemaContext schemaContext,
            ResolverBindingInfo resolverBindingInfo)
        {
            if (resolverBindingInfo is ResolverCollectionBindingInfo b)
            {
                List<FieldResolverDescriptor> descriptors =
                    CollectPossibleDescriptors(schemaContext.Types, b);

                IEnumerable<FieldResolverDescriptor> mostSpecificFieldResolvers =
                    GetMostSpecificFieldResolvers(schemaContext.Types, descriptors);

                foreach (FieldResolverDescriptor descriptor in
                    mostSpecificFieldResolvers)
                {
                    schemaContext.Resolvers.RegisterResolver(descriptor);
                }
            }
            else
            {
                throw new NotSupportedException(
                    "The binding type is not supported by this handler.");
            }
        }

        private List<FieldResolverDescriptor> CollectPossibleDescriptors(
            ITypeRegistry typeRegistry,
            ResolverCollectionBindingInfo resolverBinding)
        {
            List<FieldResolverDescriptor> descriptors =
                new List<FieldResolverDescriptor>();

            // if implicit resolver discovery is on get all possible
            // resolver members from the resolver type.
            if (resolverBinding.Behavior == BindingBehavior.Implicit)
            {
                descriptors.AddRange(_fieldResolverDiscoverer
                    .GetPossibleResolvers(resolverBinding.ResolverType,
                        resolverBinding.ObjectType,
                        resolverBinding.ObjectTypeName,
                        m => LookupFieldName(typeRegistry, m)));
            }

            if (resolverBinding.Fields.Any())
            {
                ILookup<string, FieldResolverDescriptor> descriptorLookup =
                    descriptors.ToLookup(t => t.Field.FieldName);

                IEnumerable<FieldResolverMember> selectedResolvers =
                    resolverBinding.Fields.Select(
                        t => new FieldResolverMember(
                            resolverBinding.ObjectTypeName,
                            t.FieldName, t.ResolverMember)).ToArray();

                foreach (FieldResolverDescriptor explicitDescriptor in
                    _fieldResolverDiscoverer.GetSelectedResolvers(
                        resolverBinding.ResolverType, resolverBinding.ObjectType,
                        selectedResolvers))
                {
                    // remove implicit declared descriptos for the
                    // explcitly declared ones.
                    RemoveDescriptors(descriptors,
                        descriptorLookup[explicitDescriptor.Field.FieldName]);

                    // add the explicitly declared descriptor
                    descriptors.Add(explicitDescriptor);
                }
            }

            return descriptors;
        }

        private void RemoveDescriptors(
            List<FieldResolverDescriptor> descriptors,
            IEnumerable<FieldResolverDescriptor> descriptorsToRemove)
        {
            foreach (FieldResolverDescriptor item in descriptorsToRemove)
            {
                descriptors.Remove(item);
            }
        }

        private IEnumerable<FieldResolverDescriptor> GetMostSpecificFieldResolvers(
            ITypeRegistry typeRegistry,
            IEnumerable<FieldResolverDescriptor> resolverDescriptors)
        {
            foreach (IGrouping<FieldReference, FieldResolverDescriptor> resolverGroup in
                resolverDescriptors.GroupBy(r => r.Field))
            {
                FieldReference fieldReference = resolverGroup.Key;
                if (typeRegistry.TryGetObjectTypeField(fieldReference, out ObjectField field))
                {
                    foreach (FieldResolverDescriptor resolverDescriptor in
                        resolverGroup.OrderByDescending(t => t.ArgumentCount()))
                    {
                        if (AllArgumentsMatch(field, resolverDescriptor))
                        {
                            yield return resolverDescriptor;
                            break;
                        }
                    }
                }
            }
        }

        private bool AllArgumentsMatch(ObjectField field, FieldResolverDescriptor resolverDescriptor)
        {
            foreach (FieldResolverArgumentDescriptor argumentDescriptor in
                resolverDescriptor.Arguments())
            {
                if (!field.Arguments.ContainsField(argumentDescriptor.Name))
                {
                    return false;
                }

                // TODO : Check that argument types are type compatible.
            }

            return true;
        }

        private string LookupFieldName(ITypeRegistry typeRegistry, FieldResolverMember fieldResolverMember)
        {
            foreach (ResolverCollectionBindingInfo resolverBinding in
                _resolverBindings[fieldResolverMember.TypeName])
            {
                FieldResolverBindungInfo fieldBinding = resolverBinding.Fields
                    .FirstOrDefault(t => t.FieldMember == fieldResolverMember.Member);
                if (fieldBinding != null)
                {
                    return fieldBinding.FieldName;
                }
            }

            if (typeRegistry.TryGetTypeBinding(fieldResolverMember.TypeName, out ObjectTypeBinding binding))
            {
                FieldBinding fieldBinding = binding.Fields.Values
                    .FirstOrDefault(t => t.Member == fieldResolverMember.Member);
                if (fieldBinding != null)
                {
                    return fieldBinding.Name;
                }
            }

            return fieldResolverMember.FieldName;
        }
    }
}
