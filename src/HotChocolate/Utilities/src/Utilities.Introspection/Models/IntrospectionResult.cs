namespace HotChocolate.Utilities.Introspection;

internal sealed class IntrospectionResult(IntrospectionData? data, IReadOnlyList<IntrospectionError>? errors)
{
    public IntrospectionData? Data { get; } = data;

    public IReadOnlyList<IntrospectionError>? Errors { get; } = errors;
}
