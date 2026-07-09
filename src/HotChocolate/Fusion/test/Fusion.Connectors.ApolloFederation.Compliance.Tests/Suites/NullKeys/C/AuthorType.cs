using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// Descriptor for the <c>Author</c> value type.
/// </summary>
public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<StringType>();
    }
}
