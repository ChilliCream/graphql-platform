using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field("arguments")
                .Type<ArgumentsType>()
                .Resolver(() => null);

            descriptor.Field("invalidArg")
                .Type<StringType>()
                .Argument("arg", a => a.Type<InvalidScalar>())
                .Resolver(() => null);

            descriptor.Field("anyArg")
                .Type<StringType>()
                .Argument("arg", a => a.Type<AnyType>())
                .Resolver(() => null);

            descriptor.Field(t => t.GetCatOrDog())
                .Type<CatOrDogType>();
        }
    }
}
