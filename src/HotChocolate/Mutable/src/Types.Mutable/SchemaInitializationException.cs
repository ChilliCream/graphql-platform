namespace HotChocolate.Types.Mutable;

/// <summary>
/// The exception that is thrown when an issue occurs during schema initialization.
/// </summary>
public sealed class SchemaInitializationException(string? message) : Exception(message);
