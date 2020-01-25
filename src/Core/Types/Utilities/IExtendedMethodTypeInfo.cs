using System.Collections.Generic;
using System.Reflection;

#nullable enable

namespace HotChocolate.Utilities
{
    internal interface IExtendedMethodTypeInfo
    {
        IExtendedType ReturnType { get; }

        IReadOnlyDictionary<ParameterInfo, IExtendedType> ParameterTypes { get; }
    }
}
