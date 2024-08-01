using StrawberryShake.CodeGeneration.CSharp;

namespace StrawberryShake.CodeGeneration;

public class RuntimeTypeInfo : IEquatable<RuntimeTypeInfo>
{
    public RuntimeTypeInfo(string fullName, bool isValueType = false)
    {
        var parts = fullName.Split('.');
        Name = parts.Last();
        Namespace = string.Join(".", parts.Take(parts.Length - 1));
        IsValueType = isValueType;

        if (!Namespace.StartsWith("global::"))
        {
            Namespace = "global::" + Namespace;
        }
    }

    public RuntimeTypeInfo(string name, string @namespace, bool isValueType = false)
    {
        Name = name;
        Namespace = @namespace;
        IsValueType = isValueType;

        if (!Namespace.StartsWith("global::"))
        {
            Namespace = "global::" + Namespace;
        }
    }

    public string Name { get; }

    public string Namespace { get; }

    public string FullName =>
        Namespace == "global::"
            ? Namespace + Name.ToEscapedName()
            : Namespace + "." + Name.ToEscapedName();

    public string NamespaceWithoutGlobal =>
        Namespace.Replace("global::", string.Empty);

    public bool IsValueType { get; }

    public bool Equals(RuntimeTypeInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name &&
               Namespace == other.Namespace &&
               IsValueType == other.IsValueType;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((RuntimeTypeInfo)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
    {
        return $"{Namespace}.{Name.ToEscapedName()}";
    }
}
