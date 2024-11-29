namespace HotChocolate.ApolloFederation.Types;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public sealed class PackageAttribute : Attribute
{
    internal PackageAttribute(string url, bool isFederationType = false)
    {
        Url = new(url);
        IsFederationType = isFederationType;
    }

    public PackageAttribute(string url)
    {
        Url = new(url);
    }

    public Uri Url { get; }

    internal bool IsFederationType { get; }
}
