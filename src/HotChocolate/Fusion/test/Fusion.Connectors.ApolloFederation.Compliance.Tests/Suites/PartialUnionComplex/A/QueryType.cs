using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("rootA")
            .Type<ContainerType>()
            .Resolve(_ => new Container { Id = AData.ContainerId, Actions = AData.AActions });

        descriptor
            .Field("shared")
            .Shareable()
            .Type<ContainerType>()
            .Resolve(_ => new Container { Id = AData.ContainerId, Actions = AData.SharedActions });

        descriptor
            .Field("sharedActions")
            .Shareable()
            .Type<NonNullType<ListType<NonNullType<ActionUnionType>>>>()
            .Resolve(_ => AData.AActions);
    }
}
