using System.Reflection;

#nullable enable

namespace HotChocolate.Internal;

internal sealed class ExtendedMethodInfo
{
    public ExtendedMethodInfo(
        IExtendedType returnType,
        IReadOnlyDictionary<ParameterInfo, IExtendedType> parameterTypes)
    {
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
    }

    public IExtendedType ReturnType { get; }

    public IReadOnlyDictionary<ParameterInfo, IExtendedType> ParameterTypes { get; }
}
