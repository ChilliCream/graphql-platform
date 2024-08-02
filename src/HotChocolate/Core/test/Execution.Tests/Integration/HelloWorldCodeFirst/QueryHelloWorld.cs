using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.HelloWorldCodeFirst;

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
        descriptor.Name("QueryHelloWorld");

        descriptor.Field("hello")
            .Argument("to", a => a.Type<StringType>())
            .Resolve(c => c.ArgumentValue<string>("to") ?? "world");

        descriptor.Field("state").Resolve(() => DataStore.State);
    }
}
