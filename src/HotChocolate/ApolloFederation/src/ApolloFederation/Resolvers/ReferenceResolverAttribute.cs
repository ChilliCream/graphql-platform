using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// The reference resolver enables your gateway's query planner to resolve a particular
/// entity by whatever unique identifier your other subgraphs use to reference it.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface |
    AttributeTargets.Struct |
    AttributeTargets.Method,
    AllowMultiple = true)]
public class ReferenceResolverAttribute : DescriptorAttribute
{
    public string? EntityResolver { get; set; }

    public Type? EntityResolverType { get; set; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectTypeDescriptor objectTypeDescriptor)
        {
            switch (element)
            {
                case Type type:
                    OnConfigure(objectTypeDescriptor, type);
                    return;

                case MethodInfo method:
                    OnConfigure(objectTypeDescriptor, method);
                    return;
            }
        }

        if (descriptor is IInterfaceTypeDescriptor interfaceTypeDescriptor)
        {
            switch (element)
            {
                case Type type:
                    OnConfigure(interfaceTypeDescriptor, type);
                    return;

                case MethodInfo method:
                    OnConfigure(interfaceTypeDescriptor, method);
                    return;
            }
        }
    }

    private void OnConfigure(IObjectTypeDescriptor descriptor, Type type)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor.Object<object>(descriptor);

        if (EntityResolverType is not null)
        {
            if (EntityResolver is not null)
            {
                var method = EntityResolverType.GetMethod(EntityResolver);

                if (method is null)
                {
                    throw ReferenceResolverAttribute_EntityResolverNotFound(
                        EntityResolverType,
                        EntityResolver);
                }

                entityResolverDescriptor.ResolveReferenceWith(method);
            }
            else
            {
                entityResolverDescriptor.ResolveReferenceWith(EntityResolverType);
            }
        }
        else if (EntityResolver is not null)
        {
            var method = type.GetMethod(EntityResolver);

            if (method is null)
            {
                throw ReferenceResolverAttribute_EntityResolverNotFound(
                    type,
                    EntityResolver);
            }

            entityResolverDescriptor.ResolveReferenceWith(method);
        }
        else
        {
            entityResolverDescriptor.ResolveReferenceWith(type);
        }
    }

    private static void OnConfigure(IObjectTypeDescriptor descriptor, MethodInfo method)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor.Object<object>(descriptor);
        entityResolverDescriptor.ResolveReferenceWith(method);
    }

    private void OnConfigure(IInterfaceTypeDescriptor descriptor, Type type)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor.Interface<object>(descriptor);

        if (EntityResolverType is not null)
        {
            if (EntityResolver is not null)
            {
                var method = EntityResolverType.GetMethod(EntityResolver);

                if (method is null)
                {
                    throw ReferenceResolverAttribute_EntityResolverNotFound(
                        EntityResolverType,
                        EntityResolver);
                }

                entityResolverDescriptor.ResolveReferenceWith(method);
            }
            else
            {
                entityResolverDescriptor.ResolveReferenceWith(EntityResolverType);
            }
        }
        else if (EntityResolver is not null)
        {
            var method = type.GetMethod(EntityResolver);

            if (method is null)
            {
                throw ReferenceResolverAttribute_EntityResolverNotFound(
                    type,
                    EntityResolver);
            }

            entityResolverDescriptor.ResolveReferenceWith(method);
        }
        else
        {
            entityResolverDescriptor.ResolveReferenceWith(type);
        }
    }

    private static void OnConfigure(IInterfaceTypeDescriptor descriptor, MethodInfo method)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor.Interface<object>(descriptor);
        entityResolverDescriptor.ResolveReferenceWith(method);
    }
}
