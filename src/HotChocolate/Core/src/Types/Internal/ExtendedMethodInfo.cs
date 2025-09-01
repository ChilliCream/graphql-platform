using System.Collections.Immutable;
using System.Reflection;

namespace HotChocolate.Internal;

internal sealed class ExtendedMethodInfo(
    IExtendedType returnType,
    ImmutableDictionary<ParameterInfo, IExtendedType> parameterTypes)
{
    public IExtendedType ReturnType { get; } = returnType;

    public ImmutableDictionary<ParameterInfo, IExtendedType> ParameterTypes { get; } = parameterTypes;
}
