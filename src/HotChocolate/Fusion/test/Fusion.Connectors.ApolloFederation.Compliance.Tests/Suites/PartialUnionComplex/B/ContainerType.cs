using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

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
                return new Wrapper { Actions = parent.Actions.Count > 0 ? parent.Actions : BData.BActions };
            });

        descriptor
            .Field("bWrapper")
            .Type<WrapperType>()
            .Resolve(_ => new Wrapper { Actions = BData.BActions });

        descriptor.Ignore(c => c.Actions);
    }

    private static Container? ResolveById(string id)
        => string.Equals(id, BData.ContainerId, StringComparison.Ordinal)
            ? new Container { Id = BData.ContainerId, Actions = BData.BActions }
            : null;
}
