using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

public abstract class SchemaDefinitionNodeBase
    : IHasDirectives
    , IEquatable<SchemaDefinitionNodeBase>
{
    protected SchemaDefinitionNodeBase(
        Location? location,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
    {
        Location = location;
        Directives = directives
            ?? throw new ArgumentNullException(nameof(directives));
        OperationTypes = operationTypes
            ?? throw new ArgumentNullException(nameof(operationTypes));
    }

    public abstract SyntaxKind Kind { get; }

    public Location? Location { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public IReadOnlyList<OperationTypeDefinitionNode> OperationTypes { get; }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(SchemaDefinitionNodeBase? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind
               && Directives.IsEqualTo(other.Directives)
               && OperationTypes.IsEqualTo(other.OperationTypes);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// true if the specified object  is equal to the current object; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((SchemaDefinitionNodeBase)obj);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add((int)Kind);
        hashCode.AddNodes(Directives);
        hashCode.AddNodes(OperationTypes);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(
        SchemaDefinitionNodeBase? left,
        SchemaDefinitionNodeBase? right)
        => Equals(left, right);

    public static bool operator !=(
        SchemaDefinitionNodeBase? left,
        SchemaDefinitionNodeBase? right)
        => !Equals(left, right);
}
