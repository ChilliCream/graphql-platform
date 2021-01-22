using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        private const string OperationExecutorFieldName = "_operationExecutor";
        private const string CreateRequestMethodName = "CreateRequest";

        protected override void Generate(
            CodeWriter writer,
            OperationDescriptor operationDescriptor)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            classBuilder.SetName(operationDescriptor.Name);
            constructorBuilder.SetTypeName(operationDescriptor.Name);

            AddConstructorAssignedNonNullableField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.IOperationExecutor)
                    .AddGeneric(operationDescriptor.ResultTypeReference.Name),
                OperationExecutorFieldName,
                classBuilder,
                constructorBuilder);

            MethodBuilder? executeMethod = null;
            if (operationDescriptor is not SubscriptionOperationDescriptor)
            {
                executeMethod = MethodBuilder.New()
                    .SetReturnType(
                        $"async Task<{TypeNames.IOperationResult}<" +
                        $"{operationDescriptor.ResultTypeReference.Name}>>")
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(TypeNames.Execute);
            }

            var strategyVariableName = "strategy";
            var watchMethod = MethodBuilder.New()
                .SetReturnType(
                    $"IOperationObservable<{operationDescriptor.ResultTypeReference.Name}>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(TypeNames.Watch)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetName(strategyVariableName)
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetIsNullable(true)
                                .SetName(TypeNames.ExecutionStrategy))
                        .SetDefault("null"));

            foreach (var arg in operationDescriptor.Arguments)
            {
                var typeReferenceBuilder = arg.Type.ToBuilder();

                var paramBuilder = ParameterBuilder.New()
                    .SetName(arg.Name.WithLowerFirstChar())
                    .SetType(typeReferenceBuilder);

                executeMethod?.AddParameter(paramBuilder);
                watchMethod.AddParameter(paramBuilder);
            }

            var requestVariableName = "request";
            var cancellationTokenVariableName = "cancellationToken";

            executeMethod?.AddParameter(
                ParameterBuilder.New()
                    .SetType("CancellationToken")
                    .SetName(cancellationTokenVariableName)
                    .SetDefault());

            var requestBuilder = CodeBlockBuilder.New();
            requestBuilder
                .AddCode(
                    CodeLineBuilder.New()
                        .SetLine($"var {requestVariableName} = {CreateRequestMethodName}();"));

            executeMethod?.AddCode(requestBuilder);
            watchMethod?.AddCode(requestBuilder);
            watchMethod?.AddCode(
                $"return {OperationExecutorFieldName}" +
                $".Watch({requestVariableName}, {strategyVariableName});");

            foreach (var arg in operationDescriptor.Arguments)
            {
                requestBuilder.AddCode(
                    CodeLineBuilder.New()
                        .SetLine($"request.Variables.Add(\"{arg.Name}\", {arg.Name}, );"));
            }

            executeMethod?.AddCode(
                CodeLineBuilder.New()
                    .SetLine(string.Empty));

            executeMethod?.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return await " + OperationExecutorFieldName)
                    .AddChainedCode(
                        MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName("ExecuteAsync")
                            .AddArgument(requestVariableName)
                            .AddArgument(cancellationTokenVariableName))
                    .AddChainedCode(
                        MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName("ConfigureAwait")
                            .AddArgument("false")));

            if (executeMethod is not null)
            {
                classBuilder.AddMethod(executeMethod);
            }

            if (watchMethod is not null)
            {
                classBuilder.AddMethod(watchMethod);
            }

            classBuilder.AddMethod(CreateRequestMethod(operationDescriptor));

            CodeFileBuilder
                .New()
                .SetNamespace(operationDescriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private MethodBuilder CreateRequestMethod(OperationDescriptor operationDescriptor)
        {
            string typeName = DocumentTypeNameFromOperationName(operationDescriptor.Name);

            return MethodBuilder
                .New()
                .SetName(CreateRequestMethodName)
                .SetReturnType(TypeNames.OperationRequest)
                .AddCode(
                    MethodCallBuilder.New()
                        .SetPrefix("return ")
                        .SetMethodName("new")
                        .AddArgument($"\"{operationDescriptor.ResultTypeReference.Name}\"")
                        .AddArgument($"{typeName}.Instance"));
        }
    }
}
