namespace HotChocolate.ApolloFederation;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PackageAttribute(string url) : Attribute
{
    public string Url { get; } = url;
}