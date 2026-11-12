using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<ProductType>()
            .Resolve(_ => AData.Product);
    }
}
