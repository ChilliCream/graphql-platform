namespace HotChocolate.Types;

/// <summary>
/// Represents an error that occurs when a type is initialized.
/// </summary>
public sealed class TypeInitializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeInitializationException"/>.
    /// </summary>
    public TypeInitializationException() { }

    /// <summary>
    /// Initializes a new instance of <see cref="TypeInitializationException"/>.
    /// </summary>
    public TypeInitializationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="TypeInitializationException"/>.
    /// </summary>
    public TypeInitializationException(string message, Exception inner)
        : base(message, inner) { }
}
