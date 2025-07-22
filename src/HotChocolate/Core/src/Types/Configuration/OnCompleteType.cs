using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked when a type has been completed.
/// </summary>
public delegate void OnCompleteType(
    ITypeCompletionContext context,
    TypeSystemConfiguration? definition,
    IDictionary<string, object?> contextData);
