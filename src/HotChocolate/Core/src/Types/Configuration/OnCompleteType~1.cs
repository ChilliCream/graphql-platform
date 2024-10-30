using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked when a type has been completed.
/// </summary>
/// <typeparam name="T"></typeparam>
public delegate void OnCompleteType<T>(
    ITypeCompletionContext context,
    T? definition,
    IDictionary<string, object?> contextData)
    where T : DefinitionBase;
