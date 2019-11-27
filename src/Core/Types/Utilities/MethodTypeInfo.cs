using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Utilities
{
    public class MethodTypeInfo
    {
        public MethodTypeInfo(
            TypeNullability returnType,
            IReadOnlyList<TypeNullability> parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public TypeNullability ReturnType { get; }

        public IReadOnlyList<TypeNullability> ParameterTypes { get; }
    }
}
