using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

internal readonly record struct TransportKind
{
    public TransportKind(string name)
    {
        if (!name.EqualsOrdinal("HTTP") && !name.EqualsOrdinal("WebSocket"))
        {
            throw new ArgumentException(CompositionResources.TransportKind_InvalidName, nameof(name));
        }
        
        Name = name;
    }

    public static TransportKind Http { get; } = new("HTTP");
    
    public static TransportKind WebSocket { get; } = new("WebSocket");

    public string Name { get; init; }

    public static implicit operator string(TransportKind kind) => kind.Name;
    public void Deconstruct(out string Name)
    {
        Name = this.Name;
    }
}