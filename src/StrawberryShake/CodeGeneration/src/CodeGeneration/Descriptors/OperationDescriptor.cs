using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public abstract class OperationDescriptor : ICodeDescriptor
    {
        public abstract string Name { get; }

        public TypeDescriptor ResultType { get; }

        public IReadOnlyDictionary<string, TypeDescriptor> Arguments { get; }

        public OperationDescriptor(
            TypeDescriptor resultType,
            IReadOnlyDictionary<string, TypeDescriptor> arguments)
        {
            ResultType = resultType;
            Arguments = arguments;
        }
    }
}
