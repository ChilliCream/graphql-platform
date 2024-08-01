#nullable enable

using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

public class FieldReferenceBase : IFieldReference
{
    protected FieldReferenceBase(string typeName, string fieldName)
    {
        TypeName = typeName.EnsureGraphQLName();
        FieldName = fieldName.EnsureGraphQLName();
    }

    protected FieldReferenceBase(FieldReferenceBase fieldReference)
    {
        if (fieldReference is null)
        {
            throw new ArgumentNullException(nameof(fieldReference));
        }

        TypeName = fieldReference.TypeName;
        FieldName = fieldReference.FieldName;
    }

    public string TypeName { get; }

    public string FieldName { get; }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        return IsReferenceEqualTo(obj)
            || IsEqualTo(obj as FieldReferenceBase);
    }

    protected bool IsEqualTo(FieldReferenceBase? other)
    {
        if (other is null)
        {
            return false;
        }

        if (IsReferenceEqualTo(other))
        {
            return true;
        }

        return other.TypeName.Equals(TypeName, StringComparison.Ordinal)
            && other.FieldName.Equals(FieldName, StringComparison.Ordinal);
    }

    protected bool IsReferenceEqualTo<T>(T value) where T : class
        => ReferenceEquals(this, value);

    public override int GetHashCode()
    {
        unchecked
        {
            return (TypeName.GetHashCode() * 397)
                ^ (FieldName.GetHashCode() * 17);
        }
    }

    public override string ToString()
    {
        return $"{TypeName}.{FieldName}";
    }
}
