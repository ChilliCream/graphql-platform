using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.B;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        // The audit "b" subgraph declares no root field; it only contributes the
        // shareable Response type and the partial Action union. HotChocolate
        // requires at least one query field, so a placeholder is exposed that no
        // test selects.
        descriptor
            .Field("b")
            .Type<StringType>()
            .Resolve(_ => "b");
    }
}
