using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a list result.
/// </summary>
public abstract class ListResult : ResultData
{
    /// <summary>
    /// Gets the type of the list result.
    /// </summary>
    public IType Type { get; private set; } = null!;

    /// <summary>
    /// Gets the element type of the list result.
    /// </summary>
    public IType ElementType { get; private set; } = null!;

    /// <summary>
    /// Initializes the <see cref="ListResult"/> with the specified type.
    /// </summary>
    /// <param name="type">
    /// The type of the list result.
    /// </param>
    public void Initialize(IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        ElementType = type.ElementType();
    }

    /// <summary>
    /// Resets the <see cref="ListResult"/> to its initial state.
    /// </summary>
    public override bool Reset()
    {
        Type = null!;
        ElementType = null!;

        return base.Reset();
    }
}
