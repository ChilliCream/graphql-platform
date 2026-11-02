using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes <c>b: B</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("b")
            .Type<BType>()
            .Resolve(_ => BData.ById["100"]);
    }
}
