using HotChocolate.Types;

namespace HotChocolate.Server.Template
{
    //this eventually becomes a GraphQL schema object 
    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            //here we call your logic to hydrate the object tree
            descriptor.Field(t => t.GetGreetings())
                .Type<ListType<GreetingsModelType>>().Name("greetings");
        }
    }
   
}
