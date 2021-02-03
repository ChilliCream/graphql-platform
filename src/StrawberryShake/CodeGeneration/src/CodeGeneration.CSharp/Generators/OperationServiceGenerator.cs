using System.Linq;
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

            AddConstructorAssignedField(
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
                        $"async {TypeNames.Task}<{TypeNames.IOperationResult}<" +
                        $"{operationDescriptor.ResultTypeReference.Name}>>")
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(TypeNames.Execute);
            }

            var strategyVariableName = "strategy";
            var watchMethod = MethodBuilder.New()
                .SetReturnType(
                    $"{TypeNames.IOperationObservable}<" +
                    $"{TypeNames.IOperationResult}<" +
                    $"{operationDescriptor.ResultTypeReference.Name}>>")
                .SetAccessModifier(AccessModifier.Public)
                .SetName(TypeNames.Watch);

            var createRequestMethodCall = MethodCallBuilder.New()
                .SetMethodName(CreateRequestMethodName)
                .SetDetermineStatement(false);

            foreach (var arg in operationDescriptor.Arguments)
            {
                var typeReferenceBuilder = arg.Type.ToBuilder();

                var paramName = arg.Name.WithLowerFirstChar();
                var paramBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(typeReferenceBuilder);

                createRequestMethodCall.AddArgument(paramName);
                executeMethod?.AddParameter(paramBuilder);
                watchMethod.AddParameter(paramBuilder);
            }

            var requestVariableName = "request";
            var cancellationTokenVariableName = "cancellationToken";

            executeMethod?.AddParameter(
                ParameterBuilder.New()
                    .SetType(TypeNames.CancellationToken)
                    .SetName(cancellationTokenVariableName)
                    .SetDefault());

            var requestBuilder = CodeBlockBuilder.New();
            requestBuilder
                .AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide($"var {requestVariableName}")
                        .SetRighthandSide(createRequestMethodCall));

            executeMethod?.AddCode(requestBuilder);
            watchMethod?.AddCode(requestBuilder);
            watchMethod?.AddCode(
                $"return {OperationExecutorFieldName}" +
                $".Watch({requestVariableName}, {strategyVariableName});");

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
                watchMethod.AddParameter(
                    ParameterBuilder.New()
                        .SetName(strategyVariableName)
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetIsNullable(true)
                                .SetName(TypeNames.ExecutionStrategy))
                        .SetDefault("null"));
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

            var method = MethodBuilder
                .New()
                .SetName(CreateRequestMethodName)
                .SetReturnType(TypeNames.OperationRequest);

            var requestConstructor = MethodCallBuilder.New()
                .SetPrefix("return ")
                .SetMethodName($"new {TypeNames.OperationRequest}")
                .AddArgument($"\"{operationDescriptor.ResultTypeReference.Name}\"")
                .AddArgument($"{typeName}.Instance");

            var first = true;
            foreach (var arg in operationDescriptor.Arguments)
            {
                if (first)
                {
                    var argumentsDictName = "arguments";
                    method.AddCode($"var {argumentsDictName} = new {TypeNames.Dictionary}<string, object?>();");
                    requestConstructor.AddArgument(argumentsDictName);
                }
                first = false;

                var argName = arg.Name.WithLowerFirstChar();

                method.AddParameter(ParameterBuilder.New()
                    .SetName(argName)
                    .SetType(arg.Type.ToBuilder()));

                method.AddCode(
                    CodeLineBuilder.New()
                        .SetLine($"arguments.Add(\"{arg.Name}\", {argName});"));
            }

            method.AddEmptyLine();
            method.AddCode(requestConstructor);

            return method;
        }
    }
}
