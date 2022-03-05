namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal static class CloseReasons
{
    public const int TooManyInitAttempts = 4429;
    public const int SubscriberNotUnique = 4409;
}
