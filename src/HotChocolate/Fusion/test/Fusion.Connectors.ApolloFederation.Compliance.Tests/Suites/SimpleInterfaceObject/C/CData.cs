namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

/// <summary>
/// Seed data for the <c>c</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-interface-object/data.ts</c>.
/// Subgraph <c>c</c> contributes <c>isActive</c> to <c>Account</c> via
/// <c>@interfaceObject</c> and always resolves it to <see langword="false"/>,
/// matching the audit resolver.
/// </summary>
internal static class CData
{
    public static readonly IReadOnlyList<string> AccountIds = ["u1", "u2", "u3"];
}
