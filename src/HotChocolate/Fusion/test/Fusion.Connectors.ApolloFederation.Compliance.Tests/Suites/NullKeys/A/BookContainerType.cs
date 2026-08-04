using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// Descriptor for the <c>BookContainer</c> wrapper.
/// </summary>
public sealed class BookContainerType : ObjectType<BookContainer>
{
    protected override void Configure(IObjectTypeDescriptor<BookContainer> descriptor)
    {
        descriptor.Field(c => c.Book).Type<BookType>();
    }
}
