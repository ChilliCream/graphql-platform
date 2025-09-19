using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.HelloWorldCodeFirst;

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
        descriptor.Name("Mutation");

        descriptor.Field("newState")
            .Argument("state", a => a.Type<StringType>())
            .Resolve(c => DataStore.State = c.ArgumentValue<string>("state"));
    }
}
