namespace HotChocolate.Execution.Integration.HelloWorldCodeFirst;

public class QueryHelloWorldClr
{
    private readonly DataStoreHelloWorld _dataStore;

    public QueryHelloWorldClr(DataStoreHelloWorld dataStore)
    {
        _dataStore = dataStore;
    }

    public string GetHello(string? to)
    {
        return to ?? "world";
    }

    public string GetState()
    {
        return _dataStore.State;
    }
}
