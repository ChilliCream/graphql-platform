namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Describes which kinds of policy requirements are used across the composed schema.
/// </summary>
[Flags]
internal enum FusionPolicyUsageFlags : byte
{
    None = 0,
    Authenticated = 1,
    Scopes = 2,
    Policies = 4
}
