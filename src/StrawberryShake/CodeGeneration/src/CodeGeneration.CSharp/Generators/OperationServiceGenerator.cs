using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        private static string _keyValuePair =
            TypeNames.KeyValuePair.WithGeneric(
                TypeNames.String,
                TypeNames.Object.MakeNullable());

        private const string OperationExecutorFieldName = "_operationExecutor";
        private const string CreateRequestMethodName = "CreateRequest";

        protected override void Generate(
            CodeWriter writer,
            OperationDescriptor operationDescriptor,
            out string fileName)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = operationDescriptor.Name;
            classBuilder.SetName(fileName);
            constructorBuilder.SetTypeName(fileName);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.IOperationExecutor)
                    .AddGeneric(operationDescriptor.ResultTypeReference.Name),
                OperationExecutorFieldName,
                classBuilder,
                constructorBuilder);

            /// sterializer - start
            var neededSerializers = operationDescriptor.Arguments
                .ToLookup(x => x.Type.Name)
                .Select(x => x.First())
                .ToDictionary(x => x.Type.Name);

            var code = CodeBlockBuilder.New();
            constructorBuilder.AddCode(code);

            foreach (var property in neededSerializers.Values)
            {
                var namedType = (NamedTypeDescriptor)property.Type.NamedType();
                var type = InputValueFormatterFromType(namedType);
                var typeWithNamespace =
                    namedType.Kind == TypeKind.InputType || namedType.IsEnum
                    ? type
                    : TypeNames.StrawberryshakeNamespace + "Serialization." + type;
                var parameterName = InputValueFormatterFromType(namedType).WithLowerFirstChar();
                var fieldName = "_" + parameterName;

                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetType(typeWithNamespace)
                    .SetName(type.WithLowerFirstChar());

                constructorBuilder.AddParameter(parameterBuilder);

                FieldBuilder field = FieldBuilder.New()
                    .SetName(fieldName)
                    .SetAccessModifier(AccessModifier.Private)
                    .SetType(typeWithNamespace)
                    .SetReadOnly();

                classBuilder.AddField(field);

                code.AddCode($"            {fieldName} = {parameterName};\n");
            }
            /// serializers - end

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

            foreach (var argument in operationDescriptor.Arguments)
            {
                classBuilder.AddMethod("Format" + argument.Name.WithCapitalFirstChar())
                    .AddParameter(
                        "value",
                        x => x.SetType(argument.Type.ToBuilder()))
                    .SetReturnType(TypeNames.Object.MakeNullable())
                    .SetPrivate()
                    .AddCode(
                        InputValueFormatterGenerator.GenerateSerializer(
                            argument.Type,
                            "value"));
            }

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
                .AddArgument($"\"{operationDescriptor.OperationName}\"")
                .AddArgument($"{typeName}.Instance");

            var first = true;
            foreach (var arg in operationDescriptor.Arguments)
            {
                if (first)
                {
                    var argumentsDictName = "arguments";
                    method.AddCode(
                        $"var {argumentsDictName} = new {TypeNames.Dictionary.WithGeneric(TypeNames.String, TypeNames.Object.MakeNullable())}();");
                    requestConstructor.AddArgument(argumentsDictName);
                }

                first = false;

                var argName = arg.Name.WithLowerFirstChar();

                method.AddParameter(
                    ParameterBuilder.New()
                        .SetName(argName)
                        .SetType(arg.Type.ToBuilder()));

                method.AddCode(
                    CodeLineBuilder.New()
                        .SetLine(
                            $"arguments.Add(\"{arg.Name}\", Format{arg.Name.WithCapitalFirstChar()}({argName}));"));
            }

            method.AddEmptyLine();
            method.AddCode(requestConstructor);

            return method;
        }
    }
}
