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
using InputObjectTypeDescriptor =
    StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors.InputObjectTypeDescriptor;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class OperationServiceGenerator : ClassBaseGenerator<OperationDescriptor>
{
    private const string _variables = "variables";
    private const string _files = "files";
    private const string _operationExecutor = "_operationExecutor";
    private const string operationExecutor = "operationExecutor";
    private const string _createRequest = "CreateRequest";
    private const string _strategy = "strategy";
    private const string _serializerResolver = "serializerResolver";
    private const string _request = "request";
    private const string _value = "value";
    private const string _cancellationToken = "cancellationToken";

    private static readonly string _filesType =
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
            _operationExecutor,
            operationExecutor,
            classBuilder,
            constructorBuilder);

        AddInjectedSerializers(descriptor, constructorBuilder, classBuilder);

        if (descriptor is not SubscriptionOperationDescriptor)
        {
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
            .SetMethodName(_createRequest);

        if (descriptor.Arguments.Count > 0)
        {
            createRequestCall.AddArgument($"{_variables}!");
        }

        if (descriptor.HasUpload)
        {
            createRequestCall.AddArgument(MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(_filesType));
        }

        classBuilder
            .AddMethod("Create")
            .SetReturnType(TypeNames.OperationRequest)
            .SetInterface(TypeNames.IOperationRequestFactory)
            .AddParameter(
                _variables,
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
                .AddParameter(_value, x => x.SetType(argument.Type.ToTypeReference()))
                .AddCode(GenerateSerializer(argument.Type, _value));
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

        if (!neededSerializers.Any())
        {
            return;
        }

        constructorBuilder
            .AddParameter(_serializerResolver)
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
                                        _serializerResolver,
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

    private MethodCallBuilder CreateRequestMethodCall(OperationDescriptor operationDescriptor)
    {
        var createRequestMethodCall = MethodCallBuilder
            .Inline()
            .SetMethodName(_createRequest);

        foreach (var arg in operationDescriptor.Arguments)
        {
            createRequestMethodCall.AddArgument(GetParameterName(arg.Name));
        }

        return createRequestMethodCall;
    }

    private MethodBuilder CreateWatchMethod(
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
            .SetName(_strategy)
            .SetType(TypeNames.ExecutionStrategy.MakeNullable())
            .SetDefault("null");

        return watchMethod
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {_request}")
                    .SetRightHandSide(CreateRequestMethodCall(descriptor)))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(_operationExecutor, "Watch")
                    .AddArgument(_request)
                    .AddArgument(_strategy));
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
            .AddParameter(_cancellationToken)
            .SetType(TypeNames.CancellationToken)
            .SetDefault();

        return executeMethod
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {_request}")
                    .SetRightHandSide(CreateRequestMethodCall(operationDescriptor)))
            .AddEmptyLine()
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetAwait()
                    .SetMethodName(_operationExecutor, "ExecuteAsync")
                    .AddArgument(_request)
                    .AddArgument(_cancellationToken)
                    .Chain(x => x
                        .SetMethodName(nameof(Task.ConfigureAwait))
                        .AddArgument("false")));
    }

    private MethodBuilder CreateRequestVariablesMethod(
        OperationDescriptor descriptor,
        bool hasFiles)
    {
        var typeName = CreateDocumentTypeName(descriptor.RuntimeType.Name);

        var method = MethodBuilder
            .New()
            .SetName(_createRequest)
            .SetReturnType(TypeNames.OperationRequest)
            .AddParameter(
                _variables,
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
            method.AddParameter(_files, p => p.SetType(_filesType));
            newOperationRequest.AddArgument("files: files");
        }

        if (descriptor.Arguments.Count > 0)
        {
            newOperationRequest.AddArgument("variables:" + _variables);
        }

        return method
            .AddEmptyLine()
            .AddCode(newOperationRequest);
    }

    private MethodBuilder CreateRequestMethod(OperationDescriptor descriptor)
    {
        var method = MethodBuilder
            .New()
            .SetName(_createRequest)
            .SetReturnType(TypeNames.OperationRequest);

        var createRequestWithVariables = MethodCallBuilder
            .New()
            .SetReturn()
            .SetMethodName(_createRequest);

        if (descriptor.Arguments.Count > 0)
        {
            method
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {_variables}")
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
                        .SetMethodName(_variables, nameof(Dictionary<object, object>.Add))
                        .AddArgument(arg.Name.AsStringToken())
                        .AddArgument(
                            MethodCallBuilder
                                .Inline()
                                .SetMethodName($"Format{GetPropertyName(arg.Name)}")
                                .AddArgument(argName)));
            }

            createRequestWithVariables.AddArgument(_variables);

            if (descriptor.HasUpload)
            {
                method.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {_files}")
                        .SetRightHandSide(
                            MethodCallBuilder
                                .Inline()
                                .SetNew()
                                .SetMethodName(_filesType)));

                foreach (var argument in descriptor.Arguments)
                {
                    if (argument.Type.HasUpload())
                    {
                        method.AddCode(MethodCallBuilder
                            .New()
                            .SetMethodName("MapFilesFromArgument" + GetPropertyName(argument.Name))
                            .AddArgument($"\"variables.{argument.FieldName}\"")
                            .AddArgument(argument.FieldName.ToEscapedName())
                            .AddArgument(_files));
                    }
                }

                createRequestWithVariables.AddArgument(_files);
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
            if (argument.Type.NamedType() is InputObjectTypeDescriptor { HasUpload: true, } type)
            {
                if (processed.Add(argument.Type.NamedType().Name))
                {
                    AddMapFilesOfInputTypeMethod(classBuilder, type);
                }
            }
            else if (argument.Type.NamedType() is not ScalarTypeDescriptor { Name: "Upload", })
            {
                continue;
            }

            classBuilder
                .AddMethod("MapFilesFromArgument" + GetPropertyName(argument.Name))
                .AddParameter("path", p => p.SetType(TypeNames.String))
                .AddParameter("value", p => p.SetType(argument.Type.ToTypeReference()))
                .AddParameter(_files, p => p.SetType(_filesType))
                .AddCode(BuildUploadFileMapper(argument.Type, "path", "value")!);
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
            .AddParameter(_files, p => p.SetType(_filesType));

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
        if (typeReference is NonNullTypeDescriptor { InnerType: { } it, })
        {
            typeReference = it;
        }

        ICode result;

        switch (typeReference)
        {
            case ListTypeDescriptor { InnerType: { } lt, }:
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
            case InputObjectTypeDescriptor { HasUpload: true, Name: { } inputTypeName, }:
            {
                result = MethodCallBuilder.New()
                    .SetMethodName("MapFilesFromType" + inputTypeName)
                    .AddArgument(pathVariable)
                    .AddArgument(checkedVariable)
                    .AddArgument(_files);
                break;
            }
            case ScalarTypeDescriptor { Name: "Upload", }:
            {
                return CodeBlockBuilder.New()
                    .AddCode(
                        MethodCallBuilder
                            .New()
                            .SetMethodName(_files, "Add")
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
