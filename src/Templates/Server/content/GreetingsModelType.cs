using HotChocolate.Types;

namespace HotChocolate.Server.Template
{
    //we create a special class because this will become a GraphQL schema element
    public class GreetingsModelType : ObjectType<GreetingsModel>
    {
        protected override void Configure(IObjectTypeDescriptor<GreetingsModel> descriptor)
        {
            /* Auto inferred :https://hotchocolate.io/docs/resolvers 
            descriptor.Field(t => t.Hello)
              .Type<StringType>();

            descriptor.Field(t => t.Message)
                .Type<StringType>();

            descriptor.Field(t => t.Index)
                .Type<IntType>();*/

            // simple resolver, also a field that's defined via code behind vs auto inferred from bound class
            descriptor.Field<QueryResolver>(e => e.GetMessageBasedOnIndex(default)).Name("messageFromResolver").Type<StringType>();
        }
    }
}
