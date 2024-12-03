#nullable enable
namespace HotChocolate.Resolvers;

/// <summary>
/// Replaces the arguments on the middleware context.
/// </summary>
public delegate IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
    IReadOnlyDictionary<string, ArgumentValue> currentArgumentValues);
