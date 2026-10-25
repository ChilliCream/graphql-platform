using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("getResponse")
            .Type<ResponseType>()
            .Resolve(_ => AData.Response);
    }
}
