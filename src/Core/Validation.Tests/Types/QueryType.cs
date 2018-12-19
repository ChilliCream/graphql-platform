using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field("arguments")
                .Type<ArgumentsType>()
                .Resolver(() => null);

            descriptor.Field(t => t.GetCatOrDog())
                .Type<CatOrDogType>();
        }
    }
}
