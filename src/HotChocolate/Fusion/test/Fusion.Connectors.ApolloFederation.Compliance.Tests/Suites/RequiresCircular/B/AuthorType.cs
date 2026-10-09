using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Author</c> entity in
/// subgraph <c>b</c>. <c>yearsOfExperience</c> is external.
/// </summary>
public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.YearsOfExperience).External().Type<NonNullType<IntType>>();
    }

    private static Author ResolveById(string id)
        => new() { Id = id };
}
