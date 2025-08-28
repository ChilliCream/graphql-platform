using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked when a type has been completed.
/// </summary>
public delegate void OnInitializeType(
    ITypeDiscoveryContext context,
    TypeSystemConfiguration? definition,
    IDictionary<string, object?> contextData);
