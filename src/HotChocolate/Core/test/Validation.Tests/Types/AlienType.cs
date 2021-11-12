using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class AlienType
    : ObjectType<Alien>
{
    protected override void Configure(IObjectTypeDescriptor<Alien> descriptor)
    {
        descriptor.Implements<SentientType>();
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
    }
}
