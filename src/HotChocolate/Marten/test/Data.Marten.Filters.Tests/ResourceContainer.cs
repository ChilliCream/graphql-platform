using Squadron;

namespace HotChocolate.Data;

public sealed class ResourceContainer : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _testClassInstances = 0;

    public PostgreSqlResource Resource { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_testClassInstances == 0)
            {
                await Resource.InitializeAsync();
            }
            _testClassInstances++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (--_testClassInstances == 0)
            {
                await Resource.DisposeAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
