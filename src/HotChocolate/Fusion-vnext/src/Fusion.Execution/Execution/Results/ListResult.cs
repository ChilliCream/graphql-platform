using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a list result.
/// </summary>
public abstract class ListResult : ResultData
{
    private int _defaultCapacity = 64;
    private int _maxAllowedCapacity = 512;

    /// <summary>
    /// Gets the type of the list result.
    /// </summary>
    public IType Type { get; private set; } = null!;

    /// <summary>
    /// Gets the element type of the list result.
    /// </summary>
    public IType ElementType { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the capacity of the list result.
    /// </summary>
    public abstract int Capacity { get; protected set; }

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

    internal override void SetCapacity(int capacity, int maxAllowedCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllowedCapacity, 16);

        if (capacity > maxAllowedCapacity)
        {
            throw new ArgumentException($"Capacity cannot be greater than {maxAllowedCapacity}");
        }

        _defaultCapacity = capacity;
        _maxAllowedCapacity = maxAllowedCapacity;
        Capacity = capacity;
    }

    /// <summary>
    /// Resets the <see cref="ListResult"/> to its initial state.
    /// </summary>
    public override bool Reset()
    {
        Type = null!;
        ElementType = null!;

        if (Capacity > _maxAllowedCapacity)
        {
            Capacity = _defaultCapacity;
        }

        return base.Reset();
    }
}
