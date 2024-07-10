using HotChocolate.Language;

namespace HotChocolate.Execution.Integration.DataLoader;

public class Query
{
    public Task<string> GetWithDataLoader(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return testDataLoader.LoadAsync(key, cancellationToken);
    }

    public Bar Bar => new Bar();

    public async Task<string> GetWithDataLoader2(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return await testDataLoader.LoadAsync(key, cancellationToken);
    }

    public Task<string> GetDataLoaderWithInterface(
        string key,
        FieldNode fieldSelection,
        ITestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return testDataLoader.LoadAsync(key, cancellationToken);
    }

    public async Task<string> GetWithStackedDataLoader(
        string key,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        var s = await testDataLoader.LoadAsync(key + "a", cancellationToken);
        s += await testDataLoader.LoadAsync(key + "b", cancellationToken);
        s += await testDataLoader.LoadAsync(key + "c", cancellationToken);
        s += await testDataLoader.LoadAsync(key + "d", cancellationToken);
        await Task.Delay(10, cancellationToken);
        s += await testDataLoader.LoadAsync(key + "e", cancellationToken);
        s += await testDataLoader.LoadAsync(key + "f", cancellationToken);
        s += await testDataLoader.LoadAsync(key + "g", cancellationToken);
        await Task.Delay(10, cancellationToken);
        s += await testDataLoader.LoadAsync(key + "h", cancellationToken);
        return s;
    }
}

public class Bar
{
    public Task<string> GetWithDataLoader(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return testDataLoader.LoadAsync(key, cancellationToken);
    }
}
