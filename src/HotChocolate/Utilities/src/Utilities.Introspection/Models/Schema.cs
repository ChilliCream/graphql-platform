#pragma warning disable CA1812
#nullable disable

namespace HotChocolate.Utilities.Introspection;

internal class Schema
{
    public RootTypeRef QueryType { get; set; }
    public RootTypeRef MutationType { get; set; }
    public RootTypeRef SubscriptionType { get; set; }
    public ICollection<FullType> Types { get; set; }
    public ICollection<Directive> Directives { get; set; }
}
#pragma warning restore CA1812
