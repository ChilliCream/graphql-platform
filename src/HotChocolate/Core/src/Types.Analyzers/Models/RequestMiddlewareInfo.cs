namespace HotChocolate.Types.Analyzers.Models;

public sealed class RequestMiddlewareInfo(
    string name,
    string typeName,
    string invokeMethodName,
    (string, int, int) location,
    List<RequestMiddlewareParameterInfo> ctorParameters,
    List<RequestMiddlewareParameterInfo> invokeParameters)
    : SyntaxInfo
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string InvokeMethodName { get; } = invokeMethodName;

    public (string FilePath, int LineNumber, int CharacterNumber) Location { get; } = location;

    public List<RequestMiddlewareParameterInfo> CtorParameters { get; } = ctorParameters;

    public List<RequestMiddlewareParameterInfo> InvokeParameters { get; } = invokeParameters;

    public override string OrderByKey => Name;

    public override bool Equals(object? obj)
        => obj is RequestMiddlewareInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is RequestMiddlewareInfo other && Equals(other);

    private bool Equals(RequestMiddlewareInfo other)
    {
        if (string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
            string.Equals(InvokeMethodName, other.InvokeMethodName, StringComparison.Ordinal) &&
            Location.Equals(other.Location))
        {
            if (ReferenceEquals(CtorParameters, other.CtorParameters) &&
                ReferenceEquals(InvokeParameters, other.InvokeParameters))
            {
                return true;
            }

            if (CtorParameters.Count != other.CtorParameters.Count)
            {
                return false;
            }

            if (InvokeParameters.Count != other.InvokeParameters.Count)
            {
                return false;
            }

            for (var i = 0; i < CtorParameters.Count; i++)
            {
                if (!CtorParameters[i].Equals(other.CtorParameters[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < InvokeParameters.Count; i++)
            {
                if (!InvokeParameters[i].Equals(other.InvokeParameters[i]))
                {
                    return false;
                }
            }
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(TypeName);
        hashCode.Add(InvokeMethodName);
        hashCode.Add(Location);

        foreach (var p in CtorParameters)
        {
            hashCode.Add(p);
        }

        foreach (var p in InvokeParameters)
        {
            hashCode.Add(p);
        }

        return hashCode.ToHashCode();
    }
}
