#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The binding flags can specify which type of runtime members are inferred as GraphQL fields.
/// </summary>
[Flags]
public enum FieldBindingFlags
{
    /// <summary>
    /// By default we will infer no members.
    /// </summary>
    Default = 0x00,

    /// <summary>
    /// Specifies that instance members shall be inferred as GraphQL fields.
    /// </summary>
    Instance = 0x01,

    /// <summary>
    /// Specifies that static members shall be inferred as GraphQL fields.
    /// </summary>
    Static = 0x02,

    /// <summary>
    /// Specifies that instance and static members shall be inferred as GraphQL fields.
    /// </summary>
    InstanceAndStatic = Instance | Static,
}
