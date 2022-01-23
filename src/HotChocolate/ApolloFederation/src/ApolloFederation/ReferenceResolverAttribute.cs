using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The reference resolver enables your gateway's query planner to resolve a particular
/// entity by whatever unique identifier your other subgraphs use to reference it.
/// </summary>
public class ReferenceResolverAttribute : ObjectTypeDescriptorAttribute
{
    public string? EntityResolver { get; set; }

    public Type? EntityResolverType { get; set; }

    public override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor(descriptor);

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
