using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Apollo Federation descriptor for the <c>B</c> entity in subgraph
/// <c>b</c> (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class BType : ObjectType<B>
{
    protected override void Configure(IObjectTypeDescriptor<B> descriptor)
    {
        descriptor.Name("B");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.A).Type<NonNullType<ListType<NonNullType<AType>>>>();
    }

    private static B? ResolveById(string id)
        => BData.ById.TryGetValue(id, out var b) ? b : null;
}
