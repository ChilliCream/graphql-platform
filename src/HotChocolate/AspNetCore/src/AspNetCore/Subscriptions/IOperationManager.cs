using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions;

public interface IOperationManager
    : IEnumerable<IOperationSession>
    , IDisposable
{
    bool Register(string sessionId, GraphQLRequest request);

    bool Unregister(string sessionId);
}
