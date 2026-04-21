namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Link;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>link</c> subgraph
/// (<c>@key(fields: "id") @key(fields: "id pid")</c>).
/// </summary>
public sealed record Product(string Id, string Pid);
