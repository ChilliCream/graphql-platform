namespace HotChocolate.CostAnalysis;

/// <summary>
/// The label of one condition-tree edge: either a type condition narrowing
/// the possible types, or a single Boolean literal.
/// </summary>
internal readonly struct BranchCondition : IEquatable<BranchCondition>
{
    private BranchCondition(string? typeName, BooleanLiteral? literal)
    {
        TypeName = typeName;
        Literal = literal;
    }

    /// <summary>
    /// Gets the type condition's name, or <see langword="null"/> when this
    /// edge is a Boolean literal instead.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// Gets the Boolean literal, or <see langword="null"/> when this edge is
    /// a type condition instead.
    /// </summary>
    public BooleanLiteral? Literal { get; }

    /// <summary>
    /// Creates a type-condition edge.
    /// </summary>
    public static BranchCondition Type(string typeName) => new(typeName, null);

    /// <summary>
    /// Creates a Boolean-literal edge.
    /// </summary>
    public static BranchCondition Boolean(BooleanLiteral literal) => new(null, literal);

    /// <inheritdoc />
    public bool Equals(BranchCondition other)
        => TypeName == other.TypeName && Nullable.Equals(Literal, other.Literal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BranchCondition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(TypeName, Literal);
}
