namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class CloseReasons
{
    public const int ProtocolError = 4400;
    public const int ConnectionInitWaitTimeout = 4408;
    public const int TooManyInitAttempts = 4429;
    public const int SubscriberNotUnique = 4409;
    public const int Unauthorized = 4401;
}
