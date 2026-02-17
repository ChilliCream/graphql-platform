using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class TypeRefEqualityComparer : IEqualityComparer<TypeReference>
{
    public bool Equals(TypeReference? x, TypeReference? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
        {
            return false;
        }

        return x.Equals(y);
    }

    public int GetHashCode(TypeReference obj)
        => obj.GetHashCode();
}
