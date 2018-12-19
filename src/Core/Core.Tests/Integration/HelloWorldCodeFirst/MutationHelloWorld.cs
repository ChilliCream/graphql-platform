using HotChocolate.Types;

namespace HotChocolate.Integration.HelloWorldCodeFirst
{
    public class MutationHelloWorld
        : ObjectType
    {
        public MutationHelloWorld(DataStoreHelloWorld dataStore)
        {
            DataStore = dataStore;
        }

        public DataStoreHelloWorld DataStore { get; }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("newState")
                .Argument("state", a => a.Type<StringType>())
                .Resolver(c => DataStore.State = c.Argument<string>("state"));
        }
    }
}
