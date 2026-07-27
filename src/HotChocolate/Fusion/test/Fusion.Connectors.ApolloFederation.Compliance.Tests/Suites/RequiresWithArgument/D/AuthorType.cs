using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// Type descriptor for the <c>Author</c> type in the <c>d</c>
/// subgraph. Not an entity; resolved locally from post data.
/// </summary>
public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<StringType>();
    }
}
