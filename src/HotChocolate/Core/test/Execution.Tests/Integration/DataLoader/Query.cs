using GreenDonut;
using HotChocolate.Language;

namespace HotChocolate.Execution.Integration.DataLoader;

public class Query
{
    public Task<string?> GetWithDataLoader(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return testDataLoader.LoadAsync(key, cancellationToken);
    }

    public Bar Bar => new();

    public async Task<string?> GetWithDataLoader2(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return await testDataLoader.LoadAsync(key, cancellationToken);
    }

    public Task<string?> GetDataLoaderWithInterface(
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
        var s = await testDataLoader.LoadRequiredAsync(key + "a", cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "b", cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "c", cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "d", cancellationToken);
        await Task.Delay(10, cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "e", cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "f", cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "g", cancellationToken);
        await Task.Delay(10, cancellationToken);
        s += await testDataLoader.LoadRequiredAsync(key + "h", cancellationToken);
        return s;
    }
}

public class Bar
{
    public Task<string?> GetWithDataLoader(
        string key,
        FieldNode fieldSelection,
        TestDataLoader testDataLoader,
        CancellationToken cancellationToken)
    {
        return testDataLoader.LoadAsync(key, cancellationToken);
    }
}
