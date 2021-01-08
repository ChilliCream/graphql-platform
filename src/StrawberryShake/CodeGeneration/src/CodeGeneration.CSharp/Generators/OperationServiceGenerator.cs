using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        private const string OperationStoreFieldName = "_operationStore";
        private const string OperationExecutorFieldName = "_operationExecutor";

        protected override Task WriteAsync(CodeWriter writer, OperationDescriptor operationDescriptor)
        {
            AssertNonNull(writer, operationDescriptor);


            ClassBuilder.SetName(operationDescriptor.Name);
            ConstructorBuilder.SetTypeName(operationDescriptor.Name);

            AddConstructorAssignedField(
                WellKnownNames.IOperationStore,
                OperationStoreFieldName
            );

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(WellKnownNames.IOperationExecutor)
                    .AddGeneric(operationDescriptor.ResultTypeReference.Name),
                OperationExecutorFieldName
            );

            MethodBuilder? executeMethod = null;
            if (operationDescriptor is not SubscriptionOperationDescriptor)
            {
                executeMethod = MethodBuilder.New()
                    .SetReturnType(
                        $"async Task<{WellKnownNames.IOperationResult}<{operationDescriptor.ResultTypeReference.Name}>>"
                    )
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(WellKnownNames.Execute);
            }

            var watchMethod = MethodBuilder.New()
                .SetReturnType($"IOperationObservable<{operationDescriptor.ResultTypeReference.Name}>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(WellKnownNames.Watch);

            foreach (var keyValuePair in operationDescriptor.Arguments)
            {
                var paramType = keyValuePair.Value;
                var paramBuilder = ParameterBuilder.New()
                    .SetName(keyValuePair.Key)
                    .SetType(
                        TypeReferenceBuilder.New()
                            .SetName(paramType.Name)
                            .SetIsNullable(paramType.IsNullable)
                            .SetListType(paramType.ListType)
                    );

                executeMethod?.AddParameter(paramBuilder);
                watchMethod.AddParameter(paramBuilder);
            }

            var requestVariableName = "request";
            var cancellationTokenVariableName = "cancellationToken";

            executeMethod?.AddParameter(
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
                            $"var {requestVariableName} = new {NamingConventions.RequestNameFromOperationServiceName(operationDescriptor.Name)}();"
                        )
                );

            executeMethod?.AddCode(requestBuilder);

            foreach (var keyValuePair in operationDescriptor.Arguments)
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

            executeMethod?.AddCode(
                CodeLineBuilder.New()
                    .SetLine("")
            );

            executeMethod?.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return await " + OperationExecutorFieldName)
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

            if (executeMethod is not null) ClassBuilder.AddMethod(executeMethod);
            ClassBuilder.AddMethod(watchMethod);

            return ClassBuilder.BuildAsync(writer);
        }
    }
}
