using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.TypeNames;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class DependencyInjectionGenerator : CodeGenerator<DependencyInjectionDescriptor>
{
    private const string SessionPool = "sessionPool";
    private const string Services = "services";
    private const string Strategy = "strategy";
    private const string ParentServices = "parentServices";
    private const string Profile = "profile";
    private const string ClientFactory = "clientFactory";
    private const string ServiceCollection = "serviceCollection";
    private const string Sp = "sp";
    private const string Ct = "ct";

    private static readonly string[] s_builtInSerializers =
    [
        StringSerializer,
        BooleanSerializer,
        ByteSerializer,
        ShortSerializer,
        IntSerializer,
        LongSerializer,
        FloatSerializer,
        DecimalSerializer,
        UrlSerializer,
        UUIDSerializer,
        IdSerializer,
        DateTimeSerializer,
        DateSerializer,
        LocalDateSerializer,
        LocalDateTimeSerializer,
        LocalTimeSerializer,
        ByteArraySerializer,
        TimeSpanSerializer,
        JsonSerializer
    ];

    private static readonly Dictionary<string, string> s_alternativeTypeNames = new()
    {
        ["Uuid"] = UUIDSerializer,
        ["Guid"] = UUIDSerializer,
        ["URL"] = UrlSerializer,
        ["Uri"] = UrlSerializer,
        ["URI"] = UrlSerializer,
        ["JSON"] = JsonSerializer,
        ["Json"] = JsonSerializer
    };

    protected override void Generate(
        DependencyInjectionDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = CreateServiceCollectionExtensions(descriptor.Name);
        path = DependencyInjection;
        ns = DependencyInjectionNamespace;

        var factory = ClassBuilder
            .New(fileName)
            .SetStatic()
            .SetAccessModifier(settings.AccessModifier);

        var addClientMethod = factory
            .AddMethod($"Add{descriptor.Name}")
            .SetPublic()
            .SetStatic()
            .SetReturnType(IClientBuilder.WithGeneric(descriptor.StoreAccessor.RuntimeType))
            .AddParameter(Services, x => x.SetThis().SetType(IServiceCollection))
            .AddParameter(
                Strategy,
                x => x.SetType(ExecutionStrategy)
                    .SetDefault(ExecutionStrategy + "." + "NetworkOnly"))
            .AddCode(GenerateMethodBody(settings, descriptor));

        if (descriptor.TransportProfiles.Count > 1)
        {
            addClientMethod
                .AddParameter(Profile)
                .SetType(CreateProfileEnumReference(descriptor))
                .SetDefault(CreateProfileEnumReference(descriptor) + "." +
                            descriptor.TransportProfiles[0].Name);
        }

        foreach (var profile in descriptor.TransportProfiles)
        {
            GenerateClientForProfile(settings, factory, descriptor, profile);
        }

        factory.AddClass(s_clientServiceProvider);

        factory.Build(writer);
    }

    private static void GenerateClientForProfile(
        CSharpSyntaxGeneratorSettings settings,
        ClassBuilder factory,
        DependencyInjectionDescriptor descriptor,
        TransportProfile profile)
    {
        factory
            .AddMethod("ConfigureClient" + profile.Name)
            .SetPrivate()
            .SetStatic()
            .SetReturnType(IServiceCollection)
            .AddParameter(ParentServices, x => x.SetType(TypeNames.IServiceProvider))
            .AddParameter(Services, x => x.SetType(TypeNames.ServiceCollection))
            .AddParameter(
                Strategy,
                x => x.SetType(ExecutionStrategy)
                    .SetDefault(ExecutionStrategy + "." + "NetworkOnly"))
            .AddCode(GenerateInternalMethodBody(settings, descriptor, profile));
    }

    private static ICode GenerateClientServiceProviderFactory(
        DependencyInjectionDescriptor descriptor)
    {
        var codeBuilder = CodeBlockBuilder.New();

        if (descriptor.TransportProfiles.Count == 1)
        {
            return codeBuilder
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName("ConfigureClient" + descriptor.TransportProfiles[0].Name)
                        .AddArgument(Sp)
                        .AddArgument(ServiceCollection)
                        .AddArgument(Strategy))
                .AddEmptyLine()
                .AddCode(MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName("ClientServiceProvider")
                    .SetWrapArguments()
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(BuildServiceProvider)
                        .AddArgument(ServiceCollection)));
        }

        var ifProfile = IfBuilder.New();
        var enumName = CreateProfileEnumReference(descriptor);
        for (var index = 0; index < descriptor.TransportProfiles.Count; index++)
        {
            var profile = descriptor.TransportProfiles[index];
            var currentIf = ifProfile;
            if (index != 0)
            {
                currentIf = IfBuilder.New();
                ifProfile.AddIfElse(currentIf);
            }

            currentIf
                .SetCondition($"{Profile} == {enumName}.{profile.Name}")
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName("ConfigureClient" + profile.Name)
                        .AddArgument(Sp)
                        .AddArgument(ServiceCollection)
                        .AddArgument(Strategy));
        }

        return codeBuilder
            .AddCode(ifProfile)
            .AddEmptyLine()
            .AddCode(MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName("ClientServiceProvider")
                .SetWrapArguments()
                .AddArgument(MethodCallBuilder
                    .Inline()
                    .SetMethodName(BuildServiceProvider)
                    .AddArgument(ServiceCollection)));
    }

    private static string CreateProfileEnumReference(DependencyInjectionDescriptor descriptor)
    {
        var rootNamespace = descriptor.ClientDescriptor.RuntimeType.Namespace;
        return $"{rootNamespace}.{CreateClientProfileKind(descriptor.Name)}";
    }

    private static ICode GenerateMethodBody(
        CSharpSyntaxGeneratorSettings settings,
        DependencyInjectionDescriptor descriptor) =>
        CodeBlockBuilder
            .New()
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {ServiceCollection}")
                    .SetRightHandSide(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(TypeNames.ServiceCollection)))
            .AddMethodCall(x => x
                .SetMethodName(AddSingleton)
                .AddArgument(Services)
                .AddArgument(LambdaBuilder
                    .New()
                    .SetBlock(true)
                    .AddArgument(Sp)
                    .SetCode(GenerateClientServiceProviderFactory(descriptor))))
            .AddEmptyLine()
            .AddCode(RegisterStoreAccessor(settings, descriptor.StoreAccessor))
            .AddEmptyLine()
            .ForEach(
                descriptor.Operations,
                (builder, operation) =>
                    builder.AddCode(ForwardSingletonToClientServiceProvider(
                        operation.RuntimeType.ToString())))
            .AddEmptyLine()
            .AddCode(ForwardSingletonToClientServiceProvider(
                descriptor.ClientDescriptor.RuntimeType.ToString()))
            .AddCode(ForwardSingletonToClientServiceProvider(
                descriptor.ClientDescriptor.InterfaceType.ToString()))
            .AddEmptyLine()
            .AddMethodCall(x => x
                .SetReturn()
                .SetNew()
                .SetMethodName(
                    ClientBuilder.WithGeneric(descriptor.StoreAccessor.RuntimeType))
                .AddArgument(descriptor.Name.AsStringToken())
                .AddArgument(Services)
                .AddArgument(ServiceCollection));

    private static ICode RegisterSerializerResolver() =>
        MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddGeneric(ISerializerResolver)
            .AddArgument(Services)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetCode(
                    MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(SerializerResolver)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(Concat)
                            .AddArgument(
                                MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(GetRequiredService)
                                    .SetWrapArguments()
                                    .AddGeneric(
                                        IEnumerable.WithGeneric(
                                            ISerializer))
                                    .AddArgument(ParentServices))
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(GetRequiredService)
                                .SetWrapArguments()
                                .AddGeneric(
                                    IEnumerable.WithGeneric(ISerializer))
                                .AddArgument(Sp)))));

    private static ICode RegisterStoreAccessor(
        CSharpSyntaxGeneratorSettings settings,
        StoreAccessorDescriptor storeAccessor)
    {
        if (settings.IsStoreDisabled())
        {
            return MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddArgument(Services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(Sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(storeAccessor.RuntimeType.ToString())));
        }

        return MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddArgument(Services)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetCode(MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(storeAccessor.RuntimeType.ToString())
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(Sp))
                        .AddGeneric(IOperationStore))
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(Sp))
                        .AddGeneric(IEntityStore))
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(Sp))
                        .AddGeneric(IEntityIdSerializer))
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(Sp))
                        .AddGeneric(
                            IEnumerable.WithGeneric(
                                IOperationRequestFactory)))
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(Sp))
                        .AddGeneric(
                            IEnumerable.WithGeneric(
                                IOperationResultDataFactory)))));
    }

    private static ICode ForwardSingletonToClientServiceProvider(string generic) =>
        MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddArgument(Services)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetCode(MethodCallBuilder
                    .Inline()
                    .SetMethodName(GetRequiredService)
                    .SetWrapArguments()
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .AddGeneric("ClientServiceProvider")
                        .AddArgument(Sp))
                    .AddGeneric(generic)));

    private static ICode GenerateInternalMethodBody(
        CSharpSyntaxGeneratorSettings settings,
        DependencyInjectionDescriptor descriptor,
        TransportProfile profile)
    {
        var rootNamespace = descriptor.ClientDescriptor.RuntimeType.Namespace;

        var hasSubscriptions =
            descriptor.Operations.OfType<SubscriptionOperationDescriptor>().Any();
        var hasQueries =
            descriptor.Operations.OfType<QueryOperationDescriptor>().Any();
        var hasMutations =
            descriptor.Operations.OfType<MutationOperationDescriptor>().Any();

        var body = CodeBlockBuilder
            .New()
            .AddCode(CreateBaseCode(settings));

        var generatedConnections = new HashSet<TransportType>();
        if (hasSubscriptions)
        {
            generatedConnections.Add(profile.Subscription);
            body.AddCode(
                RegisterConnection(profile.Subscription, descriptor.Name));
        }

        if (hasQueries && !generatedConnections.Contains(profile.Query))
        {
            generatedConnections.Add(profile.Query);
            body.AddCode(RegisterConnection(profile.Query, descriptor.Name));
        }

        if (hasMutations && !generatedConnections.Contains(profile.Mutation))
        {
            generatedConnections.Add(profile.Mutation);
            body.AddCode(RegisterConnection(profile.Mutation, descriptor.Name));
        }

        body.AddEmptyLine();

        foreach (INamedTypeDescriptor typeDescriptor in
            descriptor.ResultFromEntityMappers)
        {
            var namedTypeDescriptor = (INamedTypeDescriptor)typeDescriptor.NamedType();
            var className = namedTypeDescriptor.ExtractMapperName();

            var interfaceName =
                IEntityMapper.WithGeneric(
                    namedTypeDescriptor.ExtractType().ToString(),
                    $"{rootNamespace}.{typeDescriptor.RuntimeType.Name}");

            body.AddMethodCall()
                .SetMethodName(AddSingleton)
                .AddGeneric(interfaceName)
                .AddGeneric($"{CreateStateNamespace(rootNamespace)}.{className}")
                .AddArgument(Services);
        }

        body.AddEmptyLine();

        if (descriptor.Operations.Any(x => x.HasUpload))
        {
            body.AddMethodCall()
                .SetMethodName(AddSingleton)
                .AddGeneric(ISerializer)
                .AddGeneric(UploadSerializer)
                .AddArgument(Services);
        }

        foreach (var enumType in descriptor.EnumTypeDescriptor)
        {
            body.AddMethodCall()
                .SetMethodName(AddSingleton)
                .AddGeneric(ISerializer)
                .AddGeneric(CreateEnumParserName($"{rootNamespace}.{enumType.Name}"))
                .AddArgument(Services);
        }

        foreach (var serializer in s_builtInSerializers)
        {
            body.AddMethodCall()
                .SetMethodName(AddSingleton)
                .AddGeneric(ISerializer)
                .AddGeneric(serializer)
                .AddArgument(Services);
        }

        foreach (var scalarTypes in
                 descriptor.TypeDescriptors.OfType<ScalarTypeDescriptor>())
        {
            if (s_alternativeTypeNames.TryGetValue(scalarTypes.Name, out var serializer))
            {
                body.AddMethodCall()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(ISerializer)
                    .AddArgument(Services)
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(serializer)
                        .AddArgument(scalarTypes.Name.AsStringToken()));
            }
        }

        var stringTypeInfo = new RuntimeTypeInfo(TypeNames.String);
        foreach (var scalar in
                 descriptor.TypeDescriptors.OfType<ScalarTypeDescriptor>())
        {
            if (scalar.RuntimeType.Equals(stringTypeInfo) &&
                scalar.SerializationType.Equals(stringTypeInfo) &&
                !BuiltInScalarNames.IsBuiltInScalar(scalar.Name))
            {
                body.AddMethodCall()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(ISerializer)
                    .AddArgument(Services)
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(StringSerializer)
                        .AddArgument(scalar.Name.AsStringToken()));
            }
        }

        foreach (var inputTypeDescriptor in
                 descriptor.TypeDescriptors.Where(x => x.Kind is TypeKind.Input))
        {
            var formatter =
                CreateInputValueFormatter(
                    (InputObjectTypeDescriptor)inputTypeDescriptor.NamedType());

            body.AddMethodCall()
                .SetMethodName(AddSingleton)
                .AddGeneric(ISerializer)
                .AddGeneric($"{rootNamespace}.{formatter}")
                .AddArgument(Services);
        }

        body.AddCode(RegisterSerializerResolver());

        body.AddEmptyLine();

        foreach (var operation in descriptor.Operations)
        {
            if (operation.ResultTypeReference is not InterfaceTypeDescriptor typeDescriptor)
            {
                continue;
            }

            var operationKind = operation switch
            {
                SubscriptionOperationDescriptor => profile.Subscription,
                QueryOperationDescriptor => profile.Query,
                MutationOperationDescriptor => profile.Mutation,
                _ => throw ThrowHelper.DependencyInjection_InvalidOperationKind(operation)
            };

            var connectionKind = operationKind switch
            {
                TransportType.Http => IHttpConnection,
                TransportType.WebSocket => IWebSocketConnection,
                TransportType.InMemory => IInMemoryConnection,
                var v => throw ThrowHelper.DependencyInjection_InvalidTransportType(v)
            };

            var operationName = operation.Name;
            var fullName = operation.RuntimeType.ToString();
            var operationInterfaceName = operation.InterfaceType.ToString();
            var resultInterface = typeDescriptor.RuntimeType.ToString();

            // The factories are generated based on the concrete result type, which is the
            // only implementer of the result type interface.

            var factoryName =
                CreateResultFactoryName(
                    typeDescriptor.ImplementedBy.First().RuntimeType.Name);

            var builderName = CreateResultBuilderName(operationName);
            body.AddCode(
                RegisterOperation(
                    settings,
                    connectionKind,
                    fullName,
                    operationInterfaceName,
                    resultInterface,
                    $"{CreateStateNamespace(operation.RuntimeType.Namespace)}.{factoryName}",
                    $"{CreateStateNamespace(operation.RuntimeType.Namespace)}.{builderName}"));
        }

        if (settings.IsStoreEnabled())
        {
            body.AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(IEntityIdSerializer)
                    .AddGeneric(descriptor.EntityIdFactoryDescriptor.Type.ToString())
                    .AddArgument(Services));
        }

        body.AddCode(
            MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(descriptor.ClientDescriptor.RuntimeType.ToString())
                .AddArgument(Services));

        body.AddCode(
            MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(descriptor.ClientDescriptor.InterfaceType.ToString())
                .AddArgument(Services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(Sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .AddGeneric(descriptor.ClientDescriptor.RuntimeType.ToString())
                        .AddArgument(Sp))));

        body.AddLine($"return {Services};");

        return body;
    }

    private static ICode RegisterOperation(
        CSharpSyntaxGeneratorSettings settings,
        string connectionKind,
        string operationFullName,
        string operationInterfaceName,
        string resultInterface,
        string factory,
        string resultBuilder)
    {
        return CodeBlockBuilder
            .New()
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(
                        IOperationResultDataFactory.WithGeneric(resultInterface))
                    .AddGeneric(factory)
                    .AddArgument(Services))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(IOperationResultDataFactory)
                    .AddArgument(Services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(Sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(
                                IOperationResultDataFactory
                                    .WithGeneric(resultInterface))
                            .AddArgument(Sp))))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(IOperationRequestFactory)
                    .AddArgument(Services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(Sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(operationInterfaceName)
                            .AddArgument(Sp))))
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(
                    IOperationResultBuilder
                        .WithGeneric(JsonDocument, resultInterface))
                .AddGeneric(resultBuilder)
                .AddArgument(Services))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(AddSingleton)
                    .AddGeneric(IOperationExecutor.WithGeneric(resultInterface))
                    .AddArgument(Services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(Sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(settings.IsStoreEnabled()
                                ? OperationExecutor
                                : StorelessOperationExecutor)
                            .AddGeneric(JsonDocument)
                            .AddGeneric(resultInterface)
                            .AddArgument(
                                MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(GetRequiredService)
                                    .AddGeneric(connectionKind)
                                    .AddArgument(Sp))
                            .AddArgument(
                                LambdaBuilder
                                    .New()
                                    .SetCode(
                                        MethodCallBuilder
                                            .Inline()
                                            .SetMethodName(
                                                GetRequiredService)
                                            .AddGeneric(
                                                IOperationResultBuilder.WithGeneric(
                                                    JsonDocument,
                                                    resultInterface))
                                            .AddArgument(Sp)))
                            .AddArgument(
                                LambdaBuilder
                                    .New()
                                    .SetCode(
                                        MethodCallBuilder
                                            .Inline()
                                            .SetMethodName(
                                                GetRequiredService)
                                            .AddGeneric(
                                                IResultPatcher.WithGeneric(
                                                    JsonDocument))
                                            .AddArgument(Sp)))
                            .If(settings.IsStoreEnabled(),
                                x => x
                                    .AddArgument(
                                        MethodCallBuilder
                                            .Inline()
                                            .SetMethodName(GetRequiredService)
                                            .AddGeneric(IOperationStore)
                                            .AddArgument(Sp))
                                    .AddArgument(Strategy)))))
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(IResultPatcher.WithGeneric(JsonDocument))
                .AddGeneric(JsonResultPatcher)
                .AddArgument(Services))
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(operationFullName)
                .AddArgument(Services))
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(AddSingleton)
                .AddGeneric(operationInterfaceName)
                .AddArgument(Services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(Sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetMethodName(GetRequiredService)
                        .AddGeneric(operationFullName)
                        .AddArgument(Sp))));
    }

    private static ICode RegisterHttpConnection(string clientName) =>
        MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddArgument(Services)
            .AddGeneric(IHttpConnection)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetBlock(true)
                .SetCode(CodeBlockBuilder
                    .New()
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {ClientFactory}")
                        .SetRightHandSide(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(IHttpClientFactory)
                            .AddArgument(ParentServices)))
                    .AddCode(MethodCallBuilder
                        .New()
                        .SetReturn()
                        .SetNew()
                        .SetMethodName(HttpConnection)
                        .AddArgument(LambdaBuilder
                            .New()
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetMethodName(
                                    ClientFactory,
                                    "CreateClient")
                                .AddArgument(clientName.AsStringToken()))))));

    private static ICode RegisterConnection(TransportType transportProfile, string clientName)
    {
        return transportProfile switch
        {
            TransportType.WebSocket => RegisterWebSocketConnection(clientName),
            TransportType.Http => RegisterHttpConnection(clientName),
            TransportType.InMemory => RegisterInMemoryConnection(clientName),
            var v => throw ThrowHelper.DependencyInjection_InvalidTransportType(v)
        };
    }

    private static ICode RegisterInMemoryConnection(string clientName)
    {
        return MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddGeneric(IInMemoryConnection)
            .AddArgument(Services)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetBlock(true)
                .SetCode(CodeBlockBuilder
                    .New()
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {ClientFactory}")
                        .SetRightHandSide(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(IInMemoryClientFactory)
                            .AddArgument(ParentServices)))
                    .AddCode(MethodCallBuilder
                        .New()
                        .SetReturn()
                        .SetNew()
                        .SetMethodName(InMemoryConnection)
                        .AddArgument(LambdaBuilder
                            .New()
                            .SetAsync()
                            .AddArgument(Ct)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetAwait()
                                .SetMethodName(ClientFactory, "CreateAsync")
                                .AddArgument(clientName.AsStringToken())
                                .AddArgument(Ct))))));
    }

    private static ICode RegisterWebSocketConnection(string clientName) =>
        MethodCallBuilder
            .New()
            .SetMethodName(AddSingleton)
            .AddGeneric(IWebSocketConnection)
            .AddArgument(Services)
            .AddArgument(LambdaBuilder
                .New()
                .AddArgument(Sp)
                .SetBlock(true)
                .SetCode(CodeBlockBuilder
                    .New()
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLeftHandSide($"var {SessionPool}")
                        .SetRightHandSide(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(ISessionPool)
                            .AddArgument(ParentServices)))
                    .AddCode(MethodCallBuilder
                        .New()
                        .SetReturn()
                        .SetNew()
                        .SetMethodName(WebSocketConnection)
                        .AddArgument(LambdaBuilder
                            .New()
                            .SetAsync()
                            .AddArgument(Ct)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetAwait()
                                .SetMethodName(SessionPool, "CreateAsync")
                                .AddArgument(clientName.AsStringToken())
                                .AddArgument(Ct))))));

    private static ICode CreateBaseCode(CSharpSyntaxGeneratorSettings settings)
    {
        if (settings.IsStoreDisabled())
        {
            return CodeBlockBuilder.New();
        }

        return CodeBlockBuilder
            .New()
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(TryAddSingleton)
                .AddGeneric(IEntityStore)
                .AddGeneric(EntityStore)
                .AddArgument(Services))
            .AddCode(MethodCallBuilder
                .New()
                .SetMethodName(TryAddSingleton)
                .AddGeneric(IOperationStore)
                .AddArgument(Services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(Sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(OperationStore)
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(GetRequiredService)
                            .AddGeneric(IEntityStore)
                            .AddArgument(Sp)))));
    }

    private static string s_clientServiceProvider =
        @"private sealed class ClientServiceProvider
                : System.IServiceProvider
                , System.IDisposable
            {
                private readonly System.IServiceProvider _provider;

                public ClientServiceProvider(System.IServiceProvider provider)
                {
                    _provider = provider;
                }

                public object? GetService(System.Type serviceType)
                {
                    return _provider.GetService(serviceType);
                }

                public void Dispose()
                {
                    if (_provider is System.IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }";
}
