using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CircularReferenceInterface.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<ProductInterfaceType>()
            .Resolve(_ => AData.Books[0]);
    }
}
