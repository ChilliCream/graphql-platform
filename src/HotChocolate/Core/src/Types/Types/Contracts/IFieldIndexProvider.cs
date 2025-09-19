namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that have a field index.
/// </summary>
public interface IFieldIndexProvider
{
    /// <summary>
    /// The index of the field.
    /// </summary>
    int Index { get; }
}
