using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity owned by the
/// <c>price</c> subgraph (<c>@key(fields: "id tag")</c>).
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id tag")
            .ResolveReferenceWith(_ => ResolveByIdTag(default!, default));

        descriptor.Field(c => c.Id).Type<NonNullType<StringType>>();
        descriptor.Field(c => c.Tag).Type<StringType>();
    }

    private static Category ResolveByIdTag(string id, string? tag)
        => new() { Id = id, Tag = tag };
}
