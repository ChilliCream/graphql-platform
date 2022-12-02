using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Utilities;

public readonly struct FieldDependency
    : IEquatable<FieldDependency>
{
    public FieldDependency(string typeName, string fieldName)
    {
        typeName.EnsureGraphQLName(nameof(typeName));
        fieldName.EnsureGraphQLName(nameof(typeName));

        TypeName = typeName;
        FieldName = fieldName;
    }

    public string TypeName { get; }

    public string FieldName { get; }

    public bool Equals(FieldDependency other)
    {
        return other.TypeName.Equals(TypeName)
            && other.FieldName.Equals(FieldName);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        return obj is FieldDependency f && Equals(f);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TypeName.GetHashCode() * 397;
            hash = hash ^ (FieldName.GetHashCode() * 7);
            return hash;
        }
    }
}
