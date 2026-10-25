using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Author</c> entity in the
/// <c>b</c> subgraph, keyed by <c>id</c>.
/// </summary>
public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<StringType>();
    }

    private static Author ResolveById(string id) => BData.AuthorById(id);
}
