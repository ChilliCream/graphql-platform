using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors;

internal sealed class NoopDocumentationProvider : IDocumentationProvider
{
    public string? GetDescription(Type type) => null;

    public string? GetDescription(MemberInfo member) => null;

    public string? GetDescription(ParameterInfo parameter) => null;
}
