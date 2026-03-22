namespace Mocha.Mediator;

/// <summary>
/// Represents a void type since <see cref="System.Void"/> is not a valid generic type argument.
/// </summary>
/// <remarks>
/// Use <see cref="Unit"/> as the response type for commands that do not return a meaningful value.
/// </remarks>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// Gets a completed <see cref="ValueTask{Unit}"/> containing the default <see cref="Unit"/> value.
    /// </summary>
    public static readonly ValueTask<Unit> ValueTask = new(Value);

    /// <inheritdoc/>
    public bool Equals(Unit other) => true;

    /// <inheritdoc/>
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc/>
    public override int GetHashCode() => 0;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc/>
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether two <see cref="Unit"/> values are equal. Always returns <see langword="true"/>.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two <see cref="Unit"/> values are not equal. Always returns <see langword="false"/>.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    public static bool operator !=(Unit left, Unit right) => false;

    /// <summary>
    /// Gets the default and only value of <see cref="Unit"/>.
    /// </summary>
    public static Unit Value { get; }
}
