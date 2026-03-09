using HotChocolate.Language;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Allows to track field cycle depths in a GraphQL query.
/// </summary>
internal sealed class FieldDepthCycleTracker : ValidatorFeature
{
    private readonly Dictionary<SchemaCoordinate, CoordinateLimit> _coordinates = [];
    private readonly List<CoordinateLimit> _limits = [];
    private ushort? _defaultMaxAllowed;

    /// <summary>
    /// Adds a field coordinate to the tracker.
    /// </summary>
    /// <param name="coordinate">
    /// A field coordinate.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field coordinate has not reached its cycle depth limit;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Add(SchemaCoordinate coordinate)
    {
        if (_coordinates.TryGetValue(coordinate, out var limit))
        {
            return limit.Add();
        }

        if (_defaultMaxAllowed.HasValue)
        {
            _limits.TryPop(out limit);
            limit ??= new CoordinateLimit();
            limit.Reset(_defaultMaxAllowed.Value);
            _coordinates.Add(coordinate, limit);
            return limit.Add();
        }

        return true;
    }

    /// <summary>
    /// Removes a field coordinate from the tracker.
    /// </summary>
    /// <param name="coordinate">
    /// A field coordinate.
    /// </param>
    public void Remove(SchemaCoordinate coordinate)
    {
        if (_coordinates.TryGetValue(coordinate, out var limit))
        {
            limit.Remove();
        }
    }

    /// <summary>
    /// Initializes the field depth tracker with the specified limits.
    /// </summary>
    /// <param name="limits">
    /// A collection of field coordinates and their cycle depth limits.
    /// </param>
    /// <param name="defaultMaxAllowed">
    /// The default cycle depth limit for coordinates that were not explicitly defined.
    /// </param>
    public void Initialize(
        IEnumerable<(SchemaCoordinate Coordinate, ushort MaxAllowed)> limits,
        ushort? defaultMaxAllowed = null)
    {
        foreach (var (coordinate, maxAllowed) in limits)
        {
            _limits.TryPop(out var limit);
            limit ??= new CoordinateLimit();
            limit.Reset(maxAllowed);
            _coordinates.Add(coordinate, limit);
        }

        _defaultMaxAllowed = defaultMaxAllowed;
    }

    /// <summary>
    /// Resets the field depth tracker.
    /// </summary>
    protected internal override void Reset()
    {
        _limits.AddRange(_coordinates.Values);
        _coordinates.Clear();
    }
}
