using HotChocolate.Types;

namespace HotChocolate.Execution.Validation
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
