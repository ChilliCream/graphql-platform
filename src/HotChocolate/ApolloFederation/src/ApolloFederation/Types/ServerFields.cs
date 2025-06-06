using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.ApolloFederation.Types;

public static class ServerFields
{
    private static readonly _Service s_service = new();

    internal static ObjectFieldConfiguration CreateServiceField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Service);
        descriptor.Type<NonNullType<ObjectType<_Service>>>().Resolve(s_service);
        descriptor.Configuration.PureResolver = Resolve;

        static _Service Resolve(IResolverContext ctx)
            => s_service;

        return descriptor.CreateConfiguration();
    }

    internal static ObjectFieldConfiguration CreateEntitiesField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Entities);

        descriptor
            .Type<NonNullType<ListType<_EntityType>>>()
            .Argument(
                WellKnownArgumentNames.Representations,
                d => d.Type<NonNullType<ListType<NonNullType<_AnyType>>>>())
            .Resolve(
                c => EntitiesResolver.ResolveAsync(
                    c.Schema,
                    c.ArgumentValue<IReadOnlyList<Representation>>(
                        WellKnownArgumentNames.Representations),
                    c));

        return descriptor.CreateConfiguration();
    }
}
