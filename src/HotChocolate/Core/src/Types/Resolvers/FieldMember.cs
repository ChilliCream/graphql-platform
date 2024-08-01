using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers;

public sealed class FieldMember
    : FieldReferenceBase
    , IEquatable<FieldMember>
{
    private FieldReference? _fieldReference;

    public FieldMember(
        string typeName,
        string fieldName,
        MemberInfo member)
        : base(typeName, fieldName)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
    }

    public FieldMember(
        string typeName,
        string fieldName,
        Expression expression)
        : base(typeName, fieldName)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    private FieldMember(
        string typeName,
        string fieldName,
        MemberInfo? member,
        Expression? expression)
        : base(typeName, fieldName)
    {
        Member = member;
        Expression = expression;
    }

    public MemberInfo? Member { get; }

    public Expression? Expression { get; }

    public FieldMember WithTypeName(string typeName)
    {
        if (string.Equals(TypeName, typeName, StringComparison.Ordinal))
        {
            return this;
        }

        return new FieldMember(typeName, FieldName, Member, Expression);
    }

    public FieldMember WithFieldName(string fieldName)
    {
        if (string.Equals(FieldName, fieldName, StringComparison.Ordinal))
        {
            return this;
        }

        return new FieldMember(TypeName, fieldName, Member, Expression);
    }

    public FieldMember WithMember(MemberInfo member)
    {
        if (Equals(Member, member))
        {
            return this;
        }

        return new FieldMember(TypeName, FieldName, member);
    }

    public FieldMember WithExpression(Expression expression)
    {
        if (Equals(Expression, expression))
        {
            return this;
        }

        return new FieldMember(TypeName, FieldName, expression);
    }

    public bool Equals(FieldMember? other)
    {
        return IsEqualTo(other);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (IsReferenceEqualTo(obj))
        {
            return true;
        }

        return IsEqualTo(obj as FieldMember);
    }

    private bool IsEqualTo(FieldMember? other)
    {
        if (other is null)
        {
            return false;
        }

        if (IsReferenceEqualTo(other))
        {
            return true;
        }

        return base.IsEqualTo(other)
            && ReferenceEquals(other.Member, Member)
            && ReferenceEquals(other.Expression, Expression);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (base.GetHashCode() * 397)
                ^ (Member?.GetHashCode() ?? 0 * 17)
                ^ (Expression?.GetHashCode() ?? 0 * 3);
        }
    }

    public override string ToString()
    {
        return $"{base.ToString()} => {Member?.Name ?? Expression!.ToString()}";
    }

    public FieldReference ToFieldReference()
    {
        if (_fieldReference is null)
        {
            _fieldReference = new FieldReference(TypeName, FieldName);
        }
        return _fieldReference;
    }
}
