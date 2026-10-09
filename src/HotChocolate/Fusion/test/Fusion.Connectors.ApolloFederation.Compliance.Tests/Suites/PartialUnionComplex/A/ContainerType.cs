using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.A;

public sealed class ContainerType : ObjectType<Container>
{
    protected override void Configure(IObjectTypeDescriptor<Container> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("wrapper")
            .Shareable()
            .Type<WrapperType>()
            .Resolve(ctx =>
            {
                var parent = ctx.Parent<Container>();
                return new Wrapper { Actions = parent.Actions.Count > 0 ? parent.Actions : AData.AActions };
            });

        descriptor.Ignore(c => c.Actions);
    }

    private static Container? ResolveById(string id)
        => string.Equals(id, AData.ContainerId, StringComparison.Ordinal)
            ? new Container { Id = AData.ContainerId, Actions = AData.AActions }
            : null;
}
