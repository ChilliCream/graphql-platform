using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class VariableValueComparer : IEqualityComparer<ObjectValueNode>
{
    public static readonly VariableValueComparer Instance = new();

    public bool Equals(ObjectValueNode? x, ObjectValueNode? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        var xFields = x.Fields;
        var yFields = y.Fields;

        if (xFields.Count != yFields.Count)
        {
            return false;
        }

        // MapRequirements always creates fields in deterministic order.
        // We only compare values, as names are equivalent for a given operation node.
        for (var i = 0; i < xFields.Count; i++)
        {
            if (!SyntaxComparer.BySyntax.Equals(xFields[i].Value, yFields[i].Value))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(ObjectValueNode obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var hashCode = new HashCode();

        // MapRequirements creates a deterministic field order, so no field sorting is needed.
        for (var i = 0; i < obj.Fields.Count; i++)
        {
            hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Fields[i].Value));
        }

        return hashCode.ToHashCode();
    }
}
