using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation;

/// <summary>
/// The validation field info provides access to the field node and the type
/// information of the referenced field.
/// </summary>
public readonly struct FieldInfo : IEquatable<FieldInfo>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldInfo"/>
    /// </summary>
    public FieldInfo(IType declaringType, IType type, FieldNode syntaxNode)
    {
        DeclaringType = declaringType;
        Type = type;
        SyntaxNode = syntaxNode;
        ResponseName = SyntaxNode.Alias is null
            ? SyntaxNode.Name.Value
            : SyntaxNode.Alias.Value;
    }

    /// <summary>
    /// Gets the response name.
    /// </summary>
    public string ResponseName { get; }

    /// <summary>
    /// Gets the declaring type.
    /// </summary>
    public IType DeclaringType { get; }

    /// <summary>
    /// Gets the field's return type.
    /// </summary>
    public IType Type { get; }

    /// <summary>
    /// Gets the field selection syntax node.
    /// </summary>
    public FieldNode SyntaxNode { get; }

    /// <summary>
    /// Compares this field info to another field info.
    /// </summary>
    /// <param name="other">
    /// The other field info.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field infos are equal.
    /// </returns>
    public bool Equals(FieldInfo other)
        => SyntaxNode.Equals(other.SyntaxNode) &&
            DeclaringType.Equals(other.DeclaringType) &&
            Type.Equals(other.Type);

    /// <summary>
    /// Compares this field info to another object.
    /// </summary>
    /// <param name="obj">
    /// The other object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field infos are equal.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is FieldInfo other && Equals(other);

    /// <summary>
    /// Returns the hash code of this instance.
    /// </summary>
    /// <returns>
    /// The hash code of this instance.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(SyntaxNode, DeclaringType, Type);
}
