namespace HotChocolate.Types;

public class TypeInitializationException : Exception
{
    public TypeInitializationException() { }

    public TypeInitializationException(string message)
        : base(message) { }

    public TypeInitializationException(string message, Exception inner)
        : base(message, inner) { }
}
