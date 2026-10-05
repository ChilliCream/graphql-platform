namespace Mocha.Tests.Naming;

public sealed record AccountClosedEvent;

public sealed record Envelope<T>;

public abstract record CreateAccountResponse
{
    public sealed record AccountCreated : CreateAccountResponse;

    public sealed record UnexpectedError : CreateAccountResponse;

    public static class Error
    {
        public sealed record UnexpectedError;
    }
}

public abstract record DeleteAccountResponse
{
    public sealed record UnexpectedError : DeleteAccountResponse;

    public static class Error
    {
        public sealed record UnexpectedError;
    }
}

public static class CreateAccount
{
    public sealed record Command;
}

public static class DeleteAccount
{
    public sealed record Command;
}

public static class Container<T>
{
    public sealed record Item;
}

public sealed record Pair<T1, T2>;
