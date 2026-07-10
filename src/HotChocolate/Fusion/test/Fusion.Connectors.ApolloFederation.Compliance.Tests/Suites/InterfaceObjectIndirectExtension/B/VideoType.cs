using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Video</c> entity in the
/// <c>b</c> subgraph. The subgraph extends the federated <c>Video</c> type
/// (<c>extend type Video @key(fields: "id")</c>) by contributing the local
/// <c>authorName</c> field while keeping <c>id</c> as the routing key.
/// </summary>
public sealed class VideoType : ObjectType<Video>
{
    protected override void Configure(IObjectTypeDescriptor<Video> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("id");

        descriptor.Field(v => v.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("authorName")
            .Type<StringType>()
            .Resolve(_ => BData.AuthorName);
    }
}
