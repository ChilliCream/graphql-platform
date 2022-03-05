namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

public interface IOperationMessagePayload
{
    T? As<T>() where T : class;
}
