using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("_aPlaceholder")
            .Type<BooleanType>()
            .Resolve(_ => (bool?)null);
    }
}
