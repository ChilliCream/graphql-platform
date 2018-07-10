using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class AlientType
        : ObjectType<Alient>
    {
        protected override void Configure(IObjectTypeDescriptor<Alient> descriptor)
        {
            descriptor.Interface<SentientType>();
            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        }
    }
}
