using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class QueryServiceGenerator : CodeGenerator<QueryOperationDescriptor>
    {
        private const string OperationStoreFieldName = "_operationStore";
        private const string OperationStoreParamName = "operationStore";
        private const string OperationExecutorFieldName = "_operationExecutor";
        private const string OperationExecutorParamName = "operationExecutor";

        protected override Task WriteAsync(CodeWriter writer, QueryOperationDescriptor descriptor)
        {
            var classBuilder = ClassBuilder.New()
                .SetName(descriptor.Name)
                .AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetType(WellKnownNames.OperationStore)
                        .SetName(OperationStoreFieldName)
                )
                .AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetType(
                            TypeBuilder.New()
                                .SetName(WellKnownNames.OperationExecutor)
                                .AddGeneric(descriptor.ResultType.Name)
                        )
                        .SetName(OperationExecutorFieldName)
                );

            var constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(WellKnownNames.OperationStore)
                        .SetName(OperationStoreParamName)
                )
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            TypeBuilder.New()
                                .SetName(WellKnownNames.OperationExecutor)
                                .AddGeneric(descriptor.ResultType.Name)
                        )
                        .SetName(OperationExecutorParamName)
                )
                .AddCode(OperationStoreFieldName + " = " + OperationStoreParamName + ";")
                .AddCode(OperationExecutorFieldName + " = " + OperationExecutorParamName + ";");

            classBuilder.AddConstructor(constructorBuilder);

            var executeMethod = MethodBuilder.New()
                .SetReturnType($"async Task<{WellKnownNames.OperationResult}<{descriptor.ResultType.Name}>>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(WellKnownNames.Execute);

            var watchMethod = MethodBuilder.New()
                .SetReturnType($"IOperationObservable<{descriptor.ResultType.Name}>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(WellKnownNames.Watch);

            foreach (var keyValuePair in descriptor.Arguments)
            {
                var paramType = keyValuePair.Value;
                var paramBuilder = ParameterBuilder.New()
                    .SetName(keyValuePair.Key)
                    .SetType(
                        TypeBuilder.New()
                            .SetName(paramType.Name)
                            .SetIsNullable(paramType.IsNullable)
                            .SetListType(paramType.ListType)
                    );

                executeMethod.AddParameter(paramBuilder);
                watchMethod.AddParameter(paramBuilder);
            }

            var requestVariableName = "request";
            var cancellationTokenVariableName = "cancellationToken";

            executeMethod.AddParameter(
                ParameterBuilder.New()
                    .SetType("CancellationToken")
                    .SetName(cancellationTokenVariableName)
                    .SetDefault()
            );

            var requestBuilder = CodeBlockBuilder.New();
            requestBuilder
                .AddCode(
                    CodeLineBuilder.New()
                        .SetLine(
                            $"var {requestVariableName} = new {NamingConventions.RequestNameFromOperationServiceName(descriptor.Name)}();"
                        )
                );

            executeMethod.AddCode(requestBuilder);

            foreach (var keyValuePair in descriptor.Arguments)
            {
                var line = CodeLineBuilder.New();
                line.SetLine("request.Variables.Add(\"");
                line.AppendToLine(keyValuePair.Key);
                line.AppendToLine("\"");
                line.AppendToLine(", ");
                line.AppendToLine(keyValuePair.Key);
                line.AppendToLine(");");
                requestBuilder.AddCode(line);
            }

            executeMethod.AddCode(
                CodeLineBuilder.New()
                    .SetLine("")
            );

            executeMethod.AddCode(
                MethodCallBuilder.New()
                    .SetMethodName("return await " + OperationExecutorFieldName)
                    .AddChainedCode(
                        MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName("ExecuteAsync")
                            .AddArgument(requestVariableName)
                            .AddArgument(cancellationTokenVariableName)
                    )
                    .AddChainedCode(
                        MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName("ConfigureAwait")
                            .AddArgument("false")
                    )
            );

            classBuilder.AddMethod(executeMethod);
            classBuilder.AddMethod(watchMethod);

            return classBuilder.BuildAsync(writer);
        }
    }
}
