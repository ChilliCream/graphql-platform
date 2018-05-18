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
        private readonly FieldResolverBuilder _fieldResolverBuilder =
            new FieldResolverBuilder();
        private readonly IResolverBindingContext _bindingContext;

        public ResolverCollectionBindingHandler(
            IResolverBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _bindingContext = bindingContext;
        }

        public IEnumerable<Resolvers.FieldResolver> ApplyBinding(
            ResolverBindingInfo resolverBindingInfo)
        {
            if (resolverBindingInfo is ResolverCollectionBindingInfo b)
            {
                List<FieldResolverDescriptor> descriptors =
                    CollectPossibleDescriptors(b);

                IEnumerable<FieldResolverDescriptor> mostSpecificFieldResolvers =
                    GetMostSpecificFieldResolvers(descriptors);

                return _fieldResolverBuilder.Build(mostSpecificFieldResolvers);
            }

            throw new NotSupportedException(
                "The binding type is not supported by this handler.");
        }

        private List<FieldResolverDescriptor> CollectPossibleDescriptors(
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
                        _bindingContext.LookupFieldName));
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
            IEnumerable<FieldResolverDescriptor> resolverDescriptors)
        {
            foreach (var resolverGroup in resolverDescriptors.GroupBy(r => r.Field))
            {
                FieldReference fieldReference = resolverGroup.Key;
                Field field = _bindingContext.LookupField(fieldReference);
                if (field != null)
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

        private bool AllArgumentsMatch(Field field, FieldResolverDescriptor resolverDescriptor)
        {
            foreach (FieldResolverArgumentDescriptor argumentDescriptor in
                resolverDescriptor.Arguments())
            {
                if (!field.Arguments.ContainsKey(argumentDescriptor.Name))
                {
                    return false;
                }

                // TODO : Check that argument types are type compatible.
            }
            return true;
        }
    }

}
