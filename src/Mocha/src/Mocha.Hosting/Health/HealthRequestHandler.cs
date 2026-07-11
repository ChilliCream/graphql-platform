namespace Mocha.Hosting;

internal sealed class HealthRequestHandler : IEventRequestHandler<HealthRequest, HealthResponse>
{
    public ValueTask<HealthResponse> HandleAsync(HealthRequest request, CancellationToken cancellationToken)
    {
        return new(new HealthResponse("OK"));
    }
}
