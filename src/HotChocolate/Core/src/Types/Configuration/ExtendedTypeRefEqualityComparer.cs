using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class ExtendedTypeRefEqualityComparer : IEqualityComparer<ExtendedTypeReference>
{
    public bool Equals(ExtendedTypeReference? x, ExtendedTypeReference? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.Context != y.Context
            && x.Context != TypeContext.None
            && y.Context != TypeContext.None)
        {
            return false;
        }

        if (!x.Scope.EqualsOrdinal(y.Scope))
        {
            return false;
        }

        return Equals(x.Type, y.Type);
    }

    private static bool Equals(IExtendedType? x, IExtendedType? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
        {
            return false;
        }

        return ReferenceEquals(x.Type, y.Type) && x.Kind == y.Kind;
    }

    public int GetHashCode(ExtendedTypeReference obj)
    {
        unchecked
        {
            var hashCode = GetHashCode(obj.Type);

            if (obj.Scope is not null)
            {
                hashCode ^= obj.GetHashCode() * 397;
            }

            return hashCode;
        }
    }

    private static int GetHashCode(IExtendedType obj)
    {
        unchecked
        {
            var hashCode = (obj.Type.GetHashCode() * 397)
               ^ (obj.Kind.GetHashCode() * 397);

            for (var i = 0; i < obj.TypeArguments.Count; i++)
            {
                hashCode ^= GetHashCode(obj.TypeArguments[i]) * 397 * i;
            }

            return hashCode;
        }
    }
}
