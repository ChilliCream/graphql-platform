using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Mappers;
using StrawberryShake.CodeGeneration.Properties;
using static StrawberryShake.CodeGeneration.CSharp.Generators.InputValueFormatterGenerator;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using InputObjectTypeDescriptor = StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors.InputObjectTypeDescriptor;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
{
    private const string Variables = "variables";
    private const string Files = "files";
    private const string UnderscoreOperationExecutor = "_operationExecutor";
    private const string OperationExecutor = "operationExecutor";
    private const string CreateRequest = "CreateRequest";
    private const string Strategy = "strategy";
    private const string SerializerResolver = "serializerResolver";
    private const string Request = "request";
    private const string Value = "value";
    private const string CancellationToken = "cancellationToken";

    private static readonly string s_filesType =
        TypeNames.Dictionary.WithGeneric(TypeNames.String, TypeNames.Upload.MakeNullable());

    protected override void Generate(
        OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.RuntimeType.Name;
        path = null;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetComment(
                XmlCommentBuilder
                    .New()
                    .SetSummary(
                        string.Format(
                            CodeGenerationResources.OperationServiceDescriptor_Description,
                            descriptor.Name))
                    .AddCode(descriptor.BodyString))
            .AddImplements(descriptor.InterfaceType.ToString())
            .SetName(fileName);

        var constructorBuilder = classBuilder
            .AddConstructor()
            .SetTypeName(fileName);

        var resultTypeName =
            descriptor.ResultTypeReference.GetRuntimeType().Name;

        AddConstructorAssignedField(
            TypeNames.IOperationExecutor.WithGeneric(resultTypeName),
            UnderscoreOperationExecutor,
            OperationExecutor,
            classBuilder,
            constructorBuilder);

        AddInjectedSerializers(descriptor, constructorBuilder, classBuilder);

        if (descriptor is not SubscriptionOperationDescriptor)
        {
            const string arrayType = "System.Collections.Immutable.ImmutableArray<global::System.Action<global::StrawberryShake.OperationRequest>>";

            var privateConstructorBuilder = classBuilder
                .AddConstructor()
                .SetAccessModifier(AccessModifier.Private)
                .SetTypeName(fileName);

            var assignment = AssignmentBuilder
                .New()
                .SetLeftHandSide(UnderscoreOperationExecutor)
                .SetRightHandSide(OperationExecutor);

            privateConstructorBuilder
                .AddCode(assignment)
                .AddParameter(OperationExecutor, b => b.SetType(TypeNames.IOperationExecutor.WithGeneric(resultTypeName)));

            classBuilder
                .AddField()
                .SetReadOnly()
                .SetName("_configure")
                .SetType(arrayType)
                .SetValue($"{arrayType}.Empty");

            privateConstructorBuilder
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLeftHandSide("_configure")
                    .SetRightHandSide("configure"))
                .AddParameter("configure", b => b.SetType(arrayType));

            var serializerAssignments = UseInjectedSerializers(descriptor, privateConstructorBuilder);

            foreach (var method in CreateWitherMethods(descriptor, serializerAssignments))
            {
                classBuilder.AddMethod(method);
            }

            classBuilder.AddMethod(CreateExecuteMethod(descriptor, resultTypeName));
        }

        AddFileMapMethods(descriptor, classBuilder);

        classBuilder.AddMethod(CreateWatchMethod(descriptor, resultTypeName));
        classBuilder.AddMethod(CreateRequestMethod(descriptor));
        classBuilder.AddMethod(CreateRequestVariablesMethod(descriptor, descriptor.HasUpload));

        AddFormatMethods(descriptor, classBuilder);

        classBuilder
            .AddProperty("ResultType")
            .SetType(TypeNames.Type)
            .AsLambda($"typeof({resultTypeName})")
            .SetInterface(TypeNames.IOperationRequestFactory);

        var createRequestCall = MethodCallBuilder
            .New()
            .SetReturn()
            .SetMethodName(CreateRequest);

        if (descriptor.Arguments.Count > 0)
        {
            createRequestCall.AddArgument($"{Variables}!");
        }

        if (descriptor.HasUpload)
        {
            createRequestCall.AddArgument(MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(s_filesType));
        }

        classBuilder
            .AddMethod("Create")
            .SetReturnType(TypeNames.OperationRequest)
            .SetInterface(TypeNames.IOperationRequestFactory)
            .AddParameter(
                Variables,
                x => x.SetType(
                    TypeNames.IReadOnlyDictionary
                        .WithGeneric(TypeNames.String, TypeNames.Object.MakeNullable())
                        .MakeNullable()))
            .AddCode(createRequestCall);

        classBuilder.Build(writer);
    }

    private static void AddFormatMethods(
        OperationDescriptor descriptor,
        ClassBuilder classBuilder)
    {
        foreach (var argument in descriptor.Arguments)
        {
            classBuilder
                .AddMethod()
                .SetPrivate()
                .SetReturnType(TypeNames.Object.MakeNullable())
                .SetName("Format" + GetPropertyName(argument.Name))
                .AddParameter(Value, x => x.SetType(argument.Type.ToTypeReference()))
                .AddCode(GenerateSerializer(argument.Type, Value));
        }
    }

    private static void AddInjectedSerializers(
        OperationDescriptor descriptor,
        ConstructorBuilder constructorBuilder,
        ClassBuilder classBuilder)
    {
        var neededSerializers = descriptor
            .Arguments
            .GroupBy(x => x.Type.Name)
            .ToDictionary(x => x.Key, x => x.First());

        if (neededSerializers.Count == 0)
        {
            return;
        }

        constructorBuilder
            .AddParameter(SerializerResolver)
            .SetType(TypeNames.ISerializerResolver);

        foreach (var property in neededSerializers.Values)
        {
            if (property.Type.GetName() is { } name)
            {
                var fieldName = $"{GetFieldName(name)}Formatter";
                constructorBuilder
                    .AddCode(
                        AssignmentBuilder
                            .New()
                            .SetLeftHandSide(fieldName)
                            .SetRightHandSide(
                                MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(
                                        SerializerResolver,
                                        "GetInputValueFormatter")
                                    .AddArgument(name.AsStringToken())));

                classBuilder
                    .AddField()
                    .SetName(fieldName)
                    .SetAccessModifier(AccessModifier.Private)
                    .SetType(TypeNames.IInputValueFormatter)
                    .SetReadOnly();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Serializer for property {descriptor.RuntimeType.Name}." +
                    $"{property.Name} could not be created. GraphQLTypeName was empty");
            }
        }
    }

    private static string UseInjectedSerializers(
        OperationDescriptor descriptor,
        ConstructorBuilder constructorBuilder)
    {
        var neededSerializers = descriptor
            .Arguments
            .GroupBy(x => x.Type.Name)
            .ToDictionary(x => x.Key, x => x.First());

        if (neededSerializers.Count == 0)
        {
            return string.Empty;
        }

        var parameterAssignments = new StringBuilder();

        foreach (var property in neededSerializers.Values.OrderBy(x => x.Name))
        {
            if (property.Type.GetName() is { } name)
            {
                var parameterName = $"{GetParameterName(name)}Formatter";
                var fieldName = $"{GetFieldName(name)}Formatter";

                constructorBuilder
                    .AddParameter(parameterName)
                    .SetType(TypeNames.IInputValueFormatter);

                constructorBuilder
                    .AddCode(
                        AssignmentBuilder
                            .New()
                            .SetLeftHandSide(fieldName)
                            .SetRightHandSide(parameterName));

                parameterAssignments.Append(", ");
                parameterAssignments.Append(fieldName);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Serializer for property {descriptor.RuntimeType.Name}." +
                    $"{property.Name} could not be created. GraphQLTypeName was empty");
            }
        }

        return parameterAssignments.ToString();
    }

    private static MethodCallBuilder CreateRequestMethodCall(OperationDescriptor operationDescriptor)
    {
        var createRequestMethodCall = MethodCallBuilder
            .Inline()
            .SetMethodName(CreateRequest);

        foreach (var arg in operationDescriptor.Arguments)
        {
            createRequestMethodCall.AddArgument(GetParameterName(arg.Name));
        }

        return createRequestMethodCall;
    }

    private static MethodBuilder CreateWatchMethod(
        OperationDescriptor descriptor,
        string runtimeTypeName)
    {
        var watchMethod =
            MethodBuilder
                .New()
                .SetPublic()
                .SetReturnType(
                    TypeNames.IOperationObservable
                        .WithGeneric(TypeNames.IOperationResult.WithGeneric(runtimeTypeName)))
                .SetName(TypeNames.Watch);

        foreach (var arg in descriptor.Arguments)
        {
            watchMethod
                .AddParameter()
                .SetName(GetParameterName(arg.Name))
                .SetType(arg.Type.ToTypeReference());
        }

        watchMethod.AddParameter()
            .SetName(Strategy)
            .SetType(TypeNames.ExecutionStrategy.MakeNullable())
            .SetDefault("null");

        return watchMethod
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {Request}")
                    .SetRightHandSide(CreateRequestMethodCall(descriptor)))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(UnderscoreOperationExecutor, "Watch")
                    .AddArgument(Request)
                    .AddArgument(Strategy));
    }

    private MethodBuilder CreateExecuteMethod(
        OperationDescriptor operationDescriptor,
        string runtimeTypeName)
    {
        var executeMethod = MethodBuilder
            .New()
            .SetPublic()
            .SetAsync()
            .SetReturnType(
                TypeNames.Task.WithGeneric(
                    TypeNames.IOperationResult.WithGeneric(runtimeTypeName)))
            .SetName(TypeNames.Execute);

        foreach (var arg in operationDescriptor.Arguments)
        {
            executeMethod
                .AddParameter()
                .SetName(GetParameterName(arg.Name))
                .SetType(arg.Type.ToTypeReference());
        }

        executeMethod
            .AddParameter(CancellationToken)
            .SetType(TypeNames.CancellationToken)
            .SetDefault();

        return executeMethod
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {Request}")
                    .SetRightHandSide(CreateRequestMethodCall(operationDescriptor)))
            .AddEmptyLine()
            .AddCode(
                CodeInlineBuilder.From(
                    """
                    foreach (var configure in _configure)
                    {
                        configure(request);
                    }
                    """))
            .AddEmptyLine()
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetAwait()
                    .SetMethodName(UnderscoreOperationExecutor, "ExecuteAsync")
                    .AddArgument(Request)
                    .AddArgument(CancellationToken)
                    .Chain(x => x
                        .SetMethodName(nameof(Task.ConfigureAwait))
                        .AddArgument("false")));
    }

    private static IEnumerable<MethodBuilder> CreateWitherMethods(
       OperationDescriptor operationDescriptor,
       string serializerAssignments)
    {
        var withMethod = MethodBuilder
            .New()
            .SetPublic()
            .SetReturnType(operationDescriptor.InterfaceType.ToString())
            .SetName("With");

        withMethod
            .AddParameter("configure")
            .SetType("global::System.Action<global::StrawberryShake.OperationRequest>");

        yield return withMethod
            .AddCode(CodeInlineBuilder.From(
                string.Format(
                    "return new {0}(_operationExecutor, _configure.Add(configure){1});" + Environment.NewLine,
                    operationDescriptor.RuntimeType.FullName,
                    serializerAssignments)));

        var withRequestUriMethod = MethodBuilder
            .New()
            .SetPublic()
            .SetReturnType(operationDescriptor.InterfaceType.ToString())
            .SetName("WithRequestUri");

        withRequestUriMethod
            .AddParameter("requestUri")
            .SetType(TypeNames.Uri);

        yield return withRequestUriMethod
            .AddCode(CodeInlineBuilder.From(
                string.Format(
                    "return With(r => r.ContextData[\"{0}\"] = requestUri);" + Environment.NewLine,
                    "StrawberryShake.Transport.Http.HttpConnection.RequestUri")));

        var withHttpClientMethod = MethodBuilder
            .New()
            .SetPublic()
            .SetReturnType(operationDescriptor.InterfaceType.ToString())
            .SetName("WithHttpClient");

        withHttpClientMethod
            .AddParameter("httpClient")
            .SetType("global::System.Net.Http.HttpClient");

        yield return withHttpClientMethod
            .AddCode(CodeInlineBuilder.From(
                string.Format(
                    "return With(r => r.ContextData[\"{0}\"] = httpClient);" + Environment.NewLine,
                    "StrawberryShake.Transport.Http.HttpConnection.HttpClient")));
    }

    private static MethodBuilder CreateRequestVariablesMethod(
        OperationDescriptor descriptor,
        bool hasFiles)
    {
        var typeName = CreateDocumentTypeName(descriptor.RuntimeType.Name);

        var method = MethodBuilder
            .New()
            .SetName(CreateRequest)
            .SetReturnType(TypeNames.OperationRequest)
            .AddParameter(
                Variables,
                x => x.SetType(
                    TypeNames.IReadOnlyDictionary
                        .WithGeneric(TypeNames.String, TypeNames.Object.MakeNullable())
                        .MakeNullable()));

        var newOperationRequest = MethodCallBuilder
            .New()
            .SetReturn()
            .SetNew()
            .SetMethodName(TypeNames.OperationRequest)
            .AddArgument($"id: {typeName}.Instance.Hash.Value")
            .AddArgument("name: " + descriptor.Name.AsStringToken())
            .AddArgument($"document: {typeName}.Instance")
            .AddArgument($"strategy: {TypeNames.RequestStrategy}.{descriptor.Strategy}");

        if (hasFiles)
        {
            method.AddParameter(Files, p => p.SetType(s_filesType));
            newOperationRequest.AddArgument("files: files");
        }

        if (descriptor.Arguments.Count > 0)
        {
            newOperationRequest.AddArgument("variables:" + Variables);
        }

        return method
            .AddEmptyLine()
            .AddCode(newOperationRequest);
    }

    private MethodBuilder CreateRequestMethod(OperationDescriptor descriptor)
    {
        var method = MethodBuilder
            .New()
            .SetName(CreateRequest)
            .SetReturnType(TypeNames.OperationRequest);

        var createRequestWithVariables = MethodCallBuilder
            .New()
            .SetReturn()
            .SetMethodName(CreateRequest);

        if (descriptor.Arguments.Count > 0)
        {
            method
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {Variables}")
                        .SetRightHandSide(
                            MethodCallBuilder
                                .Inline()
                                .SetNew()
                                .SetMethodName(TypeNames.Dictionary)
                                .AddGeneric(TypeNames.String)
                                .AddGeneric(TypeNames.Object.MakeNullable())))
                .AddEmptyLine();

            foreach (var arg in descriptor.Arguments)
            {
                var argName = GetParameterName(arg.FieldName);

                method.AddParameter(argName, x => x.SetType(arg.Type.ToTypeReference()));

                method.AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(Variables, nameof(Dictionary<object, object>.Add))
                        .AddArgument(arg.Name.AsStringToken())
                        .AddArgument(
                            MethodCallBuilder
                                .Inline()
                                .SetMethodName($"Format{GetPropertyName(arg.Name)}")
                                .AddArgument(argName)));
            }

            createRequestWithVariables.AddArgument(Variables);

            if (descriptor.HasUpload)
            {
                method.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {Files}")
                        .SetRightHandSide(
                            MethodCallBuilder
                                .Inline()
                                .SetNew()
                                .SetMethodName(s_filesType)));

                foreach (var argument in descriptor.Arguments)
                {
                    if (argument.Type.HasUpload())
                    {
                        method.AddCode(MethodCallBuilder
                            .New()
                            .SetMethodName("MapFilesFromArgument" + GetPropertyName(argument.Name))
                            .AddArgument($"\"variables.{argument.FieldName}\"")
                            .AddArgument(argument.FieldName.ToEscapedName())
                            .AddArgument(Files));
                    }
                }

                createRequestWithVariables.AddArgument(Files);
            }
        }
        else
        {
            createRequestWithVariables.AddArgument("null");
        }

        return method
            .AddEmptyLine()
            .AddCode(createRequestWithVariables);
    }

    private static void AddFileMapMethods(
        OperationDescriptor descriptor,
        ClassBuilder classBuilder)
    {
        if (!descriptor.HasUpload)
        {
            return;
        }

        var processed = new HashSet<string>();
        foreach (var argument in descriptor.Arguments)
        {
            if (argument.Type.NamedType() is InputObjectTypeDescriptor { HasUpload: true } type)
            {
                if (processed.Add(argument.Type.NamedType().Name))
                {
                    AddMapFilesOfInputTypeMethod(classBuilder, type);
                }
            }
            else if (argument.Type.NamedType() is not ScalarTypeDescriptor { Name: "Upload" })
            {
                continue;
            }

            classBuilder
                .AddMethod("MapFilesFromArgument" + GetPropertyName(argument.Name))
                .AddParameter("path", p => p.SetType(TypeNames.String))
                .AddParameter("value", p => p.SetType(argument.Type.ToTypeReference()))
                .AddParameter(Files, p => p.SetType(s_filesType))
                .AddCode(BuildUploadFileMapper(argument.Type, "path", "value"));
        }
    }

    private static void AddMapFilesOfInputTypeMethod(
        ClassBuilder builder,
        InputObjectTypeDescriptor type)
    {
        if (!type.HasUpload)
        {
            return;
        }

        var methodBuilder = builder
            .AddMethod("MapFilesFromType" + type.Name)
            .AddParameter("path", p => p.SetType(TypeNames.String))
            .AddParameter("value", p => p.SetType(type.ToTypeReference(nonNull: true)))
            .AddParameter(Files, p => p.SetType(s_filesType));

        foreach (var field in type.Properties)
        {
            if (!field.Type.HasUpload())
            {
                continue;
            }

            methodBuilder
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var path{field.Name}")
                        .SetRightHandSide($"path + \".{field.FieldName}\""))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var value{field.Name}")
                        .SetRightHandSide($"value.{field.Name}"))
                .AddCode(
                    BuildUploadFileMapper(field.Type, $"path{field.Name}", $"value{field.Name}"))
                .AddEmptyLine();

            if (field.Type.NamedType() is InputObjectTypeDescriptor nextType)
            {
                AddMapFilesOfInputTypeMethod(builder, nextType);
            }
        }
    }

    private static ICode BuildUploadFileMapper(
        ITypeDescriptor typeReference,
        string pathVariable,
        string variable)
    {
        var checkedVariable = variable + "_i";
        if (typeReference is NonNullTypeDescriptor { InnerType: { } it })
        {
            typeReference = it;
        }

        ICode result;

        switch (typeReference)
        {
            case ListTypeDescriptor { InnerType: { } lt }:
                {
                    var innerVariable = variable + "_lt";
                    var innerPathVariable = pathVariable + "_lt";
                    var counterVariable = pathVariable + "_counter";

                    result = CodeBlockBuilder
                        .New()
                        .AddCode(AssignmentBuilder
                            .New()
                            .SetLeftHandSide($"var {counterVariable}")
                            .SetRightHandSide("0"))
                        .AddCode(ForEachBuilder
                            .New()
                            .SetLoopHeader($"var {innerVariable} in {checkedVariable}")
                            .AddCode(AssignmentBuilder
                                .New()
                                .SetLeftHandSide($"var {innerPathVariable}")
                                .SetRightHandSide($"{pathVariable} + \".\" + ({counterVariable}++)"))
                            .AddEmptyLine()
                            .AddCode(BuildUploadFileMapper(lt, innerPathVariable, innerVariable)));

                    break;
                }
            case InputObjectTypeDescriptor { HasUpload: true, Name: { } inputTypeName }:
                {
                    result = MethodCallBuilder.New()
                        .SetMethodName("MapFilesFromType" + inputTypeName)
                        .AddArgument(pathVariable)
                        .AddArgument(checkedVariable)
                        .AddArgument(Files);
                    break;
                }
            case ScalarTypeDescriptor { Name: "Upload" }:
                {
                    return CodeBlockBuilder.New()
                        .AddCode(
                            MethodCallBuilder
                                .New()
                                .SetMethodName(Files, "Add")
                                .AddArgument(pathVariable)
                                .AddArgument($"{variable} is {TypeNames.Upload} u ? u : null"));
                }
            default:
                throw ThrowHelper.OperationServiceGenerator_HasNoUploadScalar(typeReference);
        }

        return IfBuilder.New()
            .SetCondition($"{variable} is {{}} {checkedVariable}")
            .AddCode(result)
            .AddEmptyLine();
    }
}
