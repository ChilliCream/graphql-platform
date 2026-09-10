using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Author</c> entity in
/// subgraph <c>a</c>. Owns all author fields.
/// </summary>
public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<NonNullType<StringType>>();
        descriptor.Field(a => a.YearsOfExperience).Type<NonNullType<IntType>>();
    }

    private static Author? ResolveById(string id)
        => AuthorData.ById.TryGetValue(id, out var author) ? author : null;
}
