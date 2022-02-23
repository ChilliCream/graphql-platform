namespace HotChocolate.AspNetCore.Subscriptions;

public interface ISocketSession : IDisposable
{
    Task HandleAsync(CancellationToken cancellationToken);
}
