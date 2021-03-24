using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Properties;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class OperationServiceInterfaceGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        private const string _strategy = "strategy";
        private const string _cancellationToken = "cancellationToken";

        protected override void Generate(
            CodeWriter writer,
            OperationDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.InterfaceType.Name;
            path = null;

            InterfaceBuilder interfaceBuilder = InterfaceBuilder
                .New()
                .SetComment(
                    XmlCommentBuilder
                        .New()
                        .SetSummary(
                            string.Format(
                                CodeGenerationResources.OperationServiceDescriptor_Description,
                                descriptor.Name))
                        .AddCode(descriptor.BodyString))
                .AddImplements(TypeNames.IOperationRequestFactory)
                .SetName(fileName);

            var runtimeTypeName =
                descriptor.ResultTypeReference.GetRuntimeType().Name;

            if (descriptor is not SubscriptionOperationDescriptor)
            {
                interfaceBuilder.AddMethod(CreateExecuteMethod(descriptor, runtimeTypeName));
            }

            interfaceBuilder.AddMethod(CreateWatchMethod(descriptor, runtimeTypeName));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(interfaceBuilder)
                .Build(writer);
        }

        private MethodBuilder CreateWatchMethod(
            OperationDescriptor descriptor,
            string runtimeTypeName)
        {
            MethodBuilder watchMethod =
                MethodBuilder
                    .New()
                    .SetOnlyDeclaration()
                    .SetReturnType(
                        TypeNames.IOperationObservable
                            .WithGeneric(TypeNames.IOperationResult.WithGeneric(runtimeTypeName)))
                    .SetName(TypeNames.Watch);

            foreach (var arg in descriptor.Arguments)
            {
                watchMethod
                    .AddParameter()
                    .SetName(NameUtils.GetParameterName(arg.Name))
                    .SetType(arg.Type.ToTypeReference());
            }

            watchMethod.AddParameter()
                .SetName(_strategy)
                .SetType(TypeNames.ExecutionStrategy.MakeNullable())
                .SetDefault("null");

            return watchMethod;
        }

        private MethodBuilder CreateExecuteMethod(
            OperationDescriptor operationDescriptor,
            string runtimeTypeName)
        {
            MethodBuilder executeMethod = MethodBuilder
                .New()
                .SetOnlyDeclaration()
                .SetReturnType(
                    TypeNames.Task.WithGeneric(
                        TypeNames.IOperationResult.WithGeneric(runtimeTypeName)))
                .SetName(TypeNames.Execute);

            foreach (var arg in operationDescriptor.Arguments)
            {
                executeMethod
                    .AddParameter()
                    .SetName(NameUtils.GetParameterName(arg.Name))
                    .SetType(arg.Type.ToTypeReference());
            }

            executeMethod
                .AddParameter(_cancellationToken)
                .SetType(TypeNames.CancellationToken)
                .SetDefault();

            return executeMethod;
        }
    }
}
