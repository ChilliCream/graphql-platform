using HotChocolate.Types;

namespace HotChocolate.Integration.HelloWorldCodeFirst
{
    public class QueryHelloWorld
        : ObjectType
    {
        public QueryHelloWorld(DataStoreHelloWorld dataStore)
        {
            DataStore = dataStore;
        }

        public DataStoreHelloWorld DataStore { get; }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("hello")
                .Argument("to", a => a.Type<StringType>())
                .Resolver(c => c.Argument<string>("to") ?? "world");
            descriptor.Field("state").Resolver(() => DataStore.State);
        }
    }
}
