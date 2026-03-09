namespace HotChocolate.Fusion.Types;

/// <summary>
/// This interface is implemented by type system members that can be inaccessible (internal) 
/// from the composite schema and is only available in the Fusion execution schema.
/// </summary>
public interface IInaccessibleProvider
{
    /// <summary>
    /// Defines if the type system member is inaccessible.
    /// </summary>
    bool IsInaccessible { get; }
}
