using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class SentientType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Sentient");
            descriptor.Field("name").Type<NonNullType<StringType>>();
        }
    }
}
