using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class ThrowHelper
{
    public static Exception DependencyInjection_InvalidTransportType(TransportType type)
    {
        return new CodeGeneratorException(
            ErrorBuilder.New()
                .SetMessage("Transport of type {0} is not supported", type)
                .Build());
    }

    public static Exception DependencyInjection_InvalidOperationKind(OperationDescriptor descriptor)
    {
        return new CodeGeneratorException(
            ErrorBuilder
                .New()
                .SetMessage("{0} is not a valid operation kind", descriptor.GetType())
                .Build());
    }

    public static Exception OperationServiceGenerator_HasNoUploadScalar(ITypeDescriptor descriptor)
    {
        return new CodeGeneratorException(
            ErrorBuilder
                .New()
                .SetMessage(
                    "Could not generate the upload mapper for {0}. No uploadable fields found",
                    descriptor.Name)
                .Build());
    }
}
