using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation
{
    public class ReferenceResolverAttribute: ObjectTypeDescriptorAttribute
    {
        public string? EntityResolver { get; set; }

        public Type? EntityResolverType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            IEntityResolverDescriptor entityResolverDescriptor = new EntityResolverDescriptor(descriptor);

            if (EntityResolverType is not null)
            {
                if (EntityResolver is not null)
                {
                    MethodInfo? method = EntityResolverType.GetMethod(EntityResolver);

                    if (method is null)
                    {
                        throw ReferenceResolverAttribute_EntityResolverNotFound(
                            EntityResolverType,
                            EntityResolver);
                    }

                    entityResolverDescriptor.ResolveEntityWith(method);
                }
                else
                {
                    entityResolverDescriptor.ResolveEntityWith(EntityResolverType);
                }
            }
            else if (EntityResolver is not null)
            {
                MethodInfo? method = type.GetMethod(EntityResolver);

                if (method is null)
                {
                    throw ReferenceResolverAttribute_EntityResolverNotFound(
                        type,
                        EntityResolver);
                }

                entityResolverDescriptor.ResolveEntityWith(method);
            }
            else
            {
                entityResolverDescriptor.ResolveEntityWith(type);
            }
        }
    }
}
