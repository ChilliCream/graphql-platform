namespace HotChocolate.Types.Analyzers.Models;

public sealed class RequestMiddlewareInfo(
    string name,
    string typeName,
    string invokeMethodName,
    (string, int, int) location,
    List<RequestMiddlewareParameterInfo> ctorParameters,
    List<RequestMiddlewareParameterInfo> invokeParameters)
    : ISyntaxInfo, IEquatable<RequestMiddlewareInfo>
{
    public string Name { get; } = name;

    public string TypeName { get; } = typeName;

    public string InvokeMethodName { get; } = invokeMethodName;

    public (string FilePath, int LineNumber, int CharacterNumber) Location { get; } = location;

    public List<RequestMiddlewareParameterInfo> CtorParameters { get; } = ctorParameters;

    public List<RequestMiddlewareParameterInfo> InvokeParameters { get; } = invokeParameters;
    
    public bool Equals(RequestMiddlewareInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }
        
        if(Name == other.Name &&
            TypeName == other.TypeName &&
            InvokeMethodName == other.InvokeMethodName &&
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

            for (var i = 0; i < CtorParameters.Count; i++)
            {
                if (!CtorParameters[i].Equals(other.CtorParameters[i]))
                {
                    return false;
                }
            }
            
            if (InvokeParameters.Count != other.InvokeParameters.Count)
            {
                return false;
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
    
    public bool Equals(ISyntaxInfo obj)
        => ReferenceEquals(this, obj) || obj is RequestMiddlewareInfo other && Equals(other);

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is RequestMiddlewareInfo other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ InvokeMethodName.GetHashCode();
            hashCode = (hashCode * 397) ^ Location.GetHashCode();

            foreach (var p in CtorParameters)
            {
                hashCode = (hashCode * 397) ^ p.GetHashCode();
            }
            
            foreach (var p in InvokeParameters)
            {
                hashCode = (hashCode * 397) ^ p.GetHashCode();
            }
            
            return hashCode;
        }
    }
}