namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// This exception is thrown if an error happens during introspection.
/// </summary>
public class IntrospectionException : Exception
{
    public IntrospectionException() { }

    public IntrospectionException(string message)
        : base(message) { }

    public IntrospectionException(string message, Exception inner)
        : base(message, inner) { }
}
