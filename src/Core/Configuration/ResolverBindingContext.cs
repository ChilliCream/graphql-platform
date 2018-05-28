using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        private class ResolverBindingContext
            : IResolverBindingContext
        {
            private readonly SchemaContext _schemaContext;
            private readonly ILookup<string, TypeBindingInfo> _typeBindings;
            private readonly ILookup<string, ResolverCollectionBindingInfo> _resolverBindings;

            public ResolverBindingContext(
                SchemaContext schemaContext,
                IEnumerable<TypeBindingInfo> typeBindings,
                IEnumerable<ResolverCollectionBindingInfo> resolverBindings)
            {
                if (schemaContext == null)
                {
                    throw new ArgumentNullException(nameof(schemaContext));
                }

                if (typeBindings == null)
                {
                    throw new ArgumentNullException(nameof(typeBindings));
                }

                if (resolverBindings == null)
                {
                    throw new ArgumentNullException(nameof(resolverBindings));
                }

                _schemaContext = schemaContext;
                _typeBindings = typeBindings.ToLookup(t => t.Name);
                _resolverBindings = resolverBindings
                    .ToLookup(t => t.ObjectTypeName);
            }

            public Field LookupField(FieldReference fieldReference)
            {
                IOutputType type = _schemaContext.GetOutputType(
                    fieldReference.TypeName);

                if (type is ObjectType objectType && objectType.Fields
                    .TryGetValue(fieldReference.FieldName, out Field field))
                {
                    return field;
                }
                return null;
            }

            public string LookupFieldName(FieldResolverMember fieldResolverMember)
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

                TypeBindingInfo binding = _typeBindings[fieldResolverMember.TypeName].FirstOrDefault();
                if (binding != null)
                {
                    FieldBindingInfo fieldBinding = binding.Fields
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
}
