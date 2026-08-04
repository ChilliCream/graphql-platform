using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// Descriptor for the <c>FriendType</c> enum in the <c>friends</c> subgraph.
/// <c>FAMILY</c> is marked <c>@inaccessible</c> so it is hidden from the
/// supergraph clients.
/// </summary>
public sealed class FriendTypeType : EnumType<FriendTypeEnum>
{
    protected override void Configure(IEnumTypeDescriptor<FriendTypeEnum> descriptor)
    {
        descriptor.Name("FriendType");

        descriptor.Value(FriendTypeEnum.FAMILY).Inaccessible();
        descriptor.Value(FriendTypeEnum.FRIEND);
    }
}
