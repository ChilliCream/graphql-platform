using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

public class DynamicEndpointDataSourceTests
{
    [Fact]
    public void SetEndpoints_Should_LetOtherThreadsReadEndpoints_When_ChangeCallbackRuns()
    {
        // arrange
        // the change callback waits for another thread to read the endpoints
        using var dataSource = new DynamicEndpointDataSource();
        using var readCompleted = new ManualResetEventSlim();
        var endpoint = new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test");
        IReadOnlyList<Endpoint>? endpointsReadByOtherThread = null;
        var readDuringCallback = false;
        dataSource.GetChangeToken().RegisterChangeCallback(
            _ =>
            {
                Task.Run(() =>
                {
                    endpointsReadByOtherThread = dataSource.Endpoints;
                    readCompleted.Set();
                });
                readDuringCallback = readCompleted.Wait(TimeSpan.FromSeconds(5));
            },
            null);

        // act
        dataSource.SetEndpoints([endpoint]);

        // assert
        Assert.True(readDuringCallback);
        Assert.Same(endpoint, Assert.Single(endpointsReadByOtherThread!));
    }
}
