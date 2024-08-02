using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation.Types;

public static class ServerFields
{
    private static readonly _Service _service = new();

    internal static ObjectFieldDefinition CreateServiceField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Service);
        descriptor.Type<NonNullType<ObjectType<_Service>>>().Resolve(_service);
        descriptor.Definition.PureResolver = Resolve;

        static _Service Resolve(IResolverContext ctx)
            => _service;

        return descriptor.CreateDefinition();
    }

    internal static ObjectFieldDefinition CreateEntitiesField(IDescriptorContext context)
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

        return descriptor.CreateDefinition();
    }
}
