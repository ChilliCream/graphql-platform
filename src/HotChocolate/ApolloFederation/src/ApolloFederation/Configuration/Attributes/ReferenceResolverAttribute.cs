using System.Reflection;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The reference resolver enables your gateway's query planner to resolve a particular
/// entity by whatever unique identifier your other subgraphs use to reference it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ReferenceResolverAttribute : Attribute
{
    public void Configure(IDescriptorContext context, IObjectTypeDescriptor descriptor, MethodInfo method)
    {
        var entityResolverDescriptor = new EntityResolverDescriptor<object>(descriptor);
        entityResolverDescriptor.ResolveReferenceWith(method);
    }
}
