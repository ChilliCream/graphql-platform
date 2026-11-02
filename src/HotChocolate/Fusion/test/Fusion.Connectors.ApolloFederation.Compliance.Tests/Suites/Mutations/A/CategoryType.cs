using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Mutations.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Category</c> entity in the
/// <c>a</c> subgraph (<c>@key(fields: "id")</c>). Carries only the <c>id</c>
/// field; <c>name</c> is owned by subgraph <c>b</c>.
/// </summary>
public sealed class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!, default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Ignore(c => c.Name);
    }

    private static Category? ResolveById(string id, [Service] MutationsState state)
        => state.GetCategories().FirstOrDefault(
            c => string.Equals(c.Id, id, StringComparison.Ordinal));
}
