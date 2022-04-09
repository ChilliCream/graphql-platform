using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class VariableDefinitionNode
    : ISyntaxNode
    , IHasDirectives
    , IEquatable<VariableDefinitionNode>
{
    public VariableDefinitionNode(
        Location? location,
        VariableNode variable,
        ITypeNode type,
        IValueNode? defaultValue,
        IReadOnlyList<DirectiveNode> directives)
    {
        Location = location;
        Variable = variable ?? throw new ArgumentNullException(nameof(variable));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    public SyntaxKind Kind => SyntaxKind.VariableDefinition;

    public Location? Location { get; }

    public VariableNode Variable { get; }

    public ITypeNode Type { get; }

    public IValueNode? DefaultValue { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Variable;
        yield return Type;

        if (DefaultValue is not null)
        {
            yield return DefaultValue;
        }

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public VariableDefinitionNode WithLocation(Location? location)
        => new(location, Variable, Type, DefaultValue, Directives);

    public VariableDefinitionNode WithVariable(VariableNode variable)
        => new(Location, variable, Type, DefaultValue, Directives);

    public VariableDefinitionNode WithType(ITypeNode type)
        => new(Location, Variable, type, DefaultValue, Directives);

    public VariableDefinitionNode WithDefaultValue(IValueNode? defaultValue)
        => new(Location, Variable, Type, defaultValue, Directives);

    public VariableDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Variable, Type, DefaultValue, directives);

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
    public bool Equals(VariableDefinitionNode? other)
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
               && Variable.IsEqualTo(other.Variable)
               && Type.IsEqualTo(other.Type)
               && DefaultValue.IsEqualTo(other.DefaultValue)
               && Directives.IsEqualTo(other.Directives);
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
        => ReferenceEquals(this, obj) ||
            (obj is VariableDefinitionNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        hashCode.Add(Variable);
        hashCode.Add(Type);
        hashCode.Add(DefaultValue);
        hashCode.AddNodes(Directives);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(
        VariableDefinitionNode? left,
        VariableDefinitionNode? right)
        => Equals(left, right);

    /// <summary>
    /// The not equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.
    /// </returns>
    public static bool operator !=(
        VariableDefinitionNode? left,
        VariableDefinitionNode? right)
        => !Equals(left, right);
}
