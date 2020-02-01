using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Directive("executeValidation");
            descriptor.Field(t => t.SayHello(default))
                .Argument("name", a => a.Type<StringType>()
                    .Validate<string>(string.IsNullOrEmpty));
        }
    }
}
