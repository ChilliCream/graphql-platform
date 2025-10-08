#nullable enable

using System.Reflection;

namespace HotChocolate.Internal;

internal sealed class ParameterInfoComparer : IEqualityComparer<ParameterInfo>
{
    public bool Equals(ParameterInfo? x, ParameterInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.MetadataToken == y.MetadataToken
            && x.Member.Module.MetadataToken == y.Member.Module.MetadataToken;
    }

    public int GetHashCode(ParameterInfo obj)
    {
        return HashCode.Combine(obj.MetadataToken, obj.Member.Module.MetadataToken);
    }

    public static readonly ParameterInfoComparer Instance = new();
}
