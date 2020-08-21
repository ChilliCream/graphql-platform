using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Utilities
{
    internal interface IExtendedMethodTypeInfo
    {
        IExtendedType ReturnType { get; }

        IReadOnlyDictionary<ParameterInfo, IExtendedType> ParameterTypes { get; }
    }
}
