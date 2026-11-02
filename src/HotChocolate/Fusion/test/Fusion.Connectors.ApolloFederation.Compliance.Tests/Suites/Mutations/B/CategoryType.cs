using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>b</c> subgraph (<c>@key(fields: "id")</c>). Owns the <c>name</c>
/// field on top of the shared <c>id</c> contributed by subgraph <c>a</c>.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Name).Type<NonNullType<StringType>>();
    }

    private static Category? ResolveById(string id, [Service] MutationsState state)
        => state.GetCategories().FirstOrDefault(
            c => string.Equals(c.Id, id, StringComparison.Ordinal));
}
