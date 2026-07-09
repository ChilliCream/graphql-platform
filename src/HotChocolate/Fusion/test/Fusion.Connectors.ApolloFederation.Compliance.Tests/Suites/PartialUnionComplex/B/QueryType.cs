using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("rootB")
            .Type<ContainerType>()
            .Resolve(_ => new Container { Id = BData.ContainerId, Actions = BData.BActions });

        descriptor
            .Field("shared")
            .Shareable()
            .Type<ContainerType>()
            .Resolve(_ => new Container { Id = BData.ContainerId, Actions = BData.SharedActions });
    }
}
