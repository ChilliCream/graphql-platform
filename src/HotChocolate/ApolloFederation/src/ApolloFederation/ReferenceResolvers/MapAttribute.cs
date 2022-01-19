using System;

namespace HotChocolate.ApolloFederation;

[AttributeUsage(AttributeTargets.Parameter)]
public class MapAttribute : Attribute
{
    public MapAttribute(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public string Path { get; }
}
