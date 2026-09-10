using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes <c>media: Media</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaInterfaceType>()
            .Resolve(_ => AData.DefaultMedia());
    }
}
