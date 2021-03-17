using System;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.Operations;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public static class ThrowHelper
    {
        public static Exception DependencyInjection_InvalidTransportType(TransportType type)
        {
            return new CodeGeneratorException(
                ErrorBuilder.New()
                    .SetMessage("Transport of type {0} is not supported", type)
                    .Build());
        }

        public static Exception DependencyInjection_InvalidOperationKind(
            OperationDescriptor descriptor)
        {
            return new CodeGeneratorException(
                ErrorBuilder
                    .New()
                    .SetMessage("{0} is not a valid operation kind", descriptor.GetType())
                    .Build());
        }
    }
}
