using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;
using CodeGenArgument = HotChocolate.Resolvers.CodeGeneration.ArgumentDescriptor;

namespace HotChocolate.Configuration
{
    internal class ResolverCollectionBindingHandler
        : IResolverBindingHandler
    {
        private readonly ILookup<NameString, ResolverCollectionBindingInfo> _bindings;

        public ResolverCollectionBindingHandler(
            IEnumerable<ResolverCollectionBindingInfo> resolverBindings)
        {
            if (resolverBindings == null)
            {
                throw new ArgumentNullException(nameof(resolverBindings));
            }
            _bindings = resolverBindings
                .ToLookup(t => t.ObjectTypeName);
        }

        public void ApplyBinding(
            ISchemaContext schemaContext,
            ResolverBindingInfo resolverBindingInfo)
        {
            if (resolverBindingInfo is ResolverCollectionBindingInfo b)
            {
                if (b.ObjectType == null)
                {
                    b.ObjectType = typeof(object);
                }

                List<IFieldResolverDescriptor> descriptors =
                    CollectPossibleDescriptors(schemaContext.Types, b);

                IEnumerable<IFieldResolverDescriptor>
                    mostSpecificFieldResolvers =
                        GetMostSpecificFieldResolvers(
                            schemaContext.Types, descriptors);

                foreach (IFieldResolverDescriptor descriptor in
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

        private List<IFieldResolverDescriptor> CollectPossibleDescriptors(
            ITypeRegistry typeRegistry,
            ResolverCollectionBindingInfo resolverBinding)
        {
            var descriptors = new List<IFieldResolverDescriptor>();

            // if implicit resolver discovery is on get all possible
            // resolver members from the resolver type.
            if (resolverBinding.Behavior == BindingBehavior.Implicit)
            {
                descriptors.AddRange(FieldResolverDiscoverer.DiscoverResolvers(
                    resolverBinding.ResolverType,
                    resolverBinding.ObjectType,
                    resolverBinding.ObjectTypeName,
                    m => LookupFieldName(typeRegistry, m)));
            }

            if (resolverBinding.Fields.Count > 0)
            {
                ILookup<NameString, IFieldResolverDescriptor> descriptorLookup =
                    descriptors.ToLookup(t => t.Field.FieldName);

                IEnumerable<FieldMember> selectedResolvers =
                    resolverBinding.Fields.Select(
                        t => new FieldMember(
                            resolverBinding.ObjectTypeName,
                            t.FieldName, t.ResolverMember)).ToArray();

                foreach (IFieldResolverDescriptor explicitDescriptor in
                    FieldResolverDiscoverer.CreateResolverDescriptors(
                        resolverBinding.ResolverType,
                        resolverBinding.ObjectType,
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

        private static void RemoveDescriptors(
            ICollection<IFieldResolverDescriptor> descriptors,
            IEnumerable<IFieldResolverDescriptor> descriptorsToRemove)
        {
            foreach (IFieldResolverDescriptor item in descriptorsToRemove)
            {
                descriptors.Remove(item);
            }
        }

        private static IEnumerable<IFieldResolverDescriptor>
            GetMostSpecificFieldResolvers(
                ITypeRegistry typeRegistry,
                IEnumerable<IFieldResolverDescriptor> resolverDescriptors)
        {
            foreach (IGrouping<FieldReference, IFieldResolverDescriptor>
                resolverGroup in
                resolverDescriptors.GroupBy(r => r.Field.ToFieldReference()))
            {
                FieldReference fieldReference = resolverGroup.Key;
                if (typeRegistry.TryGetObjectTypeField(
                    fieldReference, out ObjectField field))
                {
                    foreach (DescriptorWithArguments descriptor in resolverGroup
                        .Select(t => new DescriptorWithArguments(t))
                        .OrderByDescending(t => t.Arguments.Count))
                    {
                        if (AllArgumentsMatch(field, descriptor.Arguments))
                        {
                            yield return descriptor.ResolverDescriptor;
                            break;
                        }
                    }
                }
            }
        }

        private static bool AllArgumentsMatch(
            ObjectField field,
            IReadOnlyCollection<CodeGenArgument> arguments)
        {
            foreach (CodeGenArgument argumentDescriptor in arguments)
            {
                if (!field.Arguments.ContainsField(argumentDescriptor.Name))
                {
                    return false;
                }

                // TODO : Check that argument types are type compatible.
            }

            return true;
        }

        private string LookupFieldName(
            ITypeRegistry typeRegistry,
            FieldMember fieldResolverMember)
        {
            MemberInfo member = fieldResolverMember.Member;

            foreach (ResolverCollectionBindingInfo resolverBinding in
                _bindings[fieldResolverMember.TypeName])
            {
                FieldResolverBindungInfo fieldBinding = resolverBinding.Fields
                    .FirstOrDefault(t => t.FieldMember == member);
                if (fieldBinding != null)
                {
                    return fieldBinding.FieldName;
                }
            }

            if (typeRegistry.TryGetTypeBinding(
                fieldResolverMember.TypeName,
                out ObjectTypeBinding binding))
            {
                FieldBinding fieldBinding = binding.Fields.Values
                    .FirstOrDefault(t => t.Member == member);
                if (fieldBinding != null)
                {
                    return fieldBinding.Name;
                }
            }

            return fieldResolverMember.FieldName;
        }

        private class DescriptorWithArguments
        {
            public DescriptorWithArguments(
                IFieldResolverDescriptor resolverDescriptor)
            {
                ResolverDescriptor = resolverDescriptor;
                Arguments = resolverDescriptor.Arguments
                    .Where(t => t.Kind == ArgumentKind.Argument)
                    .ToArray();
            }

            public IFieldResolverDescriptor ResolverDescriptor { get; }
            public IReadOnlyCollection<CodeGenArgument> Arguments { get; }
        }
    }
}
