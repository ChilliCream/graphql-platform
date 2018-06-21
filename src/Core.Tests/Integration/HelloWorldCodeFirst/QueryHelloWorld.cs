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
            descriptor.Field("hello").Resolver(() => "world");
            descriptor.Field("state").Resolver(() => DataStore.State);
        }
    }
}
