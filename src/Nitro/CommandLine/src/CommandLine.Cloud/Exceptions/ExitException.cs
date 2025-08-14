namespace ChilliCream.Nitro.CLI.Exceptions;

internal sealed class ExitException : Exception
{
    public ExitException() : base("")
    {
    }

    public ExitException(string message) : base(message)
    {
    }
}
