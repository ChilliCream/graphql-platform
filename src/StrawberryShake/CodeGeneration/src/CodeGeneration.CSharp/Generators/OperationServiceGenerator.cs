using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        private const string OperationExecutorFieldName = "_operationExecutor";
        private const string CreateRequestMethodName = "CreateRequest";

        protected override Task WriteAsync(
            CodeWriter writer,
            OperationDescriptor operationDescriptor)
        {
            AssertNonNull(
                writer,
                operationDescriptor);

            ClassBuilder.SetName(operationDescriptor.Name);
            ConstructorBuilder.SetTypeName(operationDescriptor.Name);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(WellKnownNames.IOperationExecutor)
                    .AddGeneric(operationDescriptor.ResultTypeReference.Name),
                OperationExecutorFieldName);

            MethodBuilder? executeMethod = null;
            if (operationDescriptor is not SubscriptionOperationDescriptor)
            {
                executeMethod = MethodBuilder.New()
                    .SetReturnType(
                        $"async Task<{WellKnownNames.IOperationResult}<" +
                        $"{operationDescriptor.ResultTypeReference.Name}>>")
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(WellKnownNames.Execute);
            }

            var strategyVariableName = "strategy";
            var watchMethod = MethodBuilder.New()
                .SetReturnType(
                    $"IOperationObservable<{operationDescriptor.ResultTypeReference.Name}>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(WellKnownNames.Watch)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetName(strategyVariableName)
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetIsNullable(true)
                                .SetName(WellKnownNames.ExecutionStrategy))
                        .SetDefault("null"));

            foreach (var arg in operationDescriptor.Arguments)
            {
                var paramBuilder = ParameterBuilder.New()
                    .SetName(arg.Name.WithLowerFirstChar())
                    .SetType(
                        TypeReferenceBuilder.New()
                            .SetName(arg.Type.Name)
                            .SetIsNullable(arg.Type.IsNullable));

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
                ClassBuilder.AddMethod(executeMethod);
            }

            if (watchMethod is not null)
            {
                ClassBuilder.AddMethod(watchMethod);
            }

            ClassBuilder.AddMethod(CreateRequestMethod(operationDescriptor));

            return CodeFileBuilder.New()
                .SetNamespace(operationDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        private MethodBuilder CreateRequestMethod(OperationDescriptor operationDescriptor)
        {
            string typeName = DocumentTypeNameFromOperationName(operationDescriptor.Name);

            return MethodBuilder.New()
                .SetName(CreateRequestMethodName)
                .SetReturnType(WellKnownNames.OperationRequest)
                .AddCode(
                    MethodCallBuilder.New()
                        .SetPrefix("return ")
                        .SetMethodName("new")
                        .AddArgument($"\"{operationDescriptor.ResultTypeReference.Name}\"")
                        .AddArgument($"{typeName}.Instance"));
        }
    }
}
