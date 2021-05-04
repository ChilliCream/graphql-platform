using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class DependencyInjectionGenerator : CodeGenerator<DependencyInjectionDescriptor>
    {
        private const string _sessionPool = "sessionPool";
        private const string _services = "services";
        private const string _strategy = "strategy";
        private const string _parentServices = "parentServices";
        private const string _profile = "profile";
        private const string _clientFactory = "clientFactory";
        private const string _serviceCollection = "serviceCollection";
        private const string _sp = "sp";
        private const string _ct = "ct";

        private static readonly string[] _serializers =
        {
            TypeNames.StringSerializer,
            TypeNames.BooleanSerializer,
            TypeNames.ByteSerializer,
            TypeNames.ShortSerializer,
            TypeNames.IntSerializer,
            TypeNames.LongSerializer,
            TypeNames.FloatSerializer,
            TypeNames.DecimalSerializer,
            TypeNames.UrlSerializer,
            TypeNames.UuidSerializer,
            TypeNames.IdSerializer,
            TypeNames.DateTimeSerializer,
            TypeNames.DateSerializer,
            TypeNames.ByteArraySerializer,
            TypeNames.TimeSpanSerializer
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
            ns = TypeNames.DependencyInjectionNamespace;

            ClassBuilder factory = ClassBuilder
                .New(fileName)
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public);

            MethodBuilder addClientMethod = factory
                .AddMethod($"Add{descriptor.Name}")
                .SetPublic()
                .SetStatic()
                .SetReturnType(
                    TypeNames.IClientBuilder.WithGeneric(descriptor.StoreAccessor.RuntimeType))
                .AddParameter(
                    _services,
                    x => x.SetThis().SetType(TypeNames.IServiceCollection))
                .AddParameter(
                    _strategy,
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(TypeNames.ExecutionStrategy + "." + "NetworkOnly"))
                .AddCode(GenerateMethodBody(settings, descriptor));

            if (descriptor.TransportProfiles.Count > 1)
            {
                addClientMethod
                    .AddParameter(_profile)
                    .SetType(CreateProfileEnumReference(descriptor))
                    .SetDefault(CreateProfileEnumReference(descriptor) + "." +
                        descriptor.TransportProfiles[0].Name);
            }

            foreach (var profile in descriptor.TransportProfiles)
            {
                GenerateClientForProfile(settings, factory, descriptor, profile);
            }


            factory.AddClass(_clientServiceProvider);

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
                .SetReturnType(TypeNames.IServiceCollection)
                .AddParameter(_parentServices, x => x.SetType(TypeNames.IServiceProvider))
                .AddParameter(_services, x => x.SetType(TypeNames.ServiceCollection))
                .AddParameter(
                    _strategy,
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(TypeNames.ExecutionStrategy + "." + "NetworkOnly"))
                .AddCode(GenerateInternalMethodBody(settings, descriptor, profile));
        }

        private static ICode GenerateClientServiceProviderFactory(
            DependencyInjectionDescriptor descriptor)
        {
            CodeBlockBuilder codeBuilder = CodeBlockBuilder.New();

            if (descriptor.TransportProfiles.Count == 1)
            {
                return codeBuilder
                    .AddCode(
                        MethodCallBuilder
                            .New()
                            .SetMethodName("ConfigureClient" + descriptor.TransportProfiles[0].Name)
                            .AddArgument(_sp)
                            .AddArgument(_serviceCollection)
                            .AddArgument(_strategy))
                    .AddEmptyLine()
                    .AddCode(MethodCallBuilder
                        .New()
                        .SetReturn()
                        .SetNew()
                        .SetMethodName("ClientServiceProvider")
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.BuildServiceProvider)
                            .AddArgument(_serviceCollection)));
            }

            IfBuilder ifProfile = IfBuilder.New();

            var enumName = CreateProfileEnumReference(descriptor);
            for (var index = 0; index < descriptor.TransportProfiles.Count; index++)
            {
                TransportProfile profile = descriptor.TransportProfiles[index];
                IfBuilder currentIf = ifProfile;
                if (index != 0)
                {
                    currentIf = IfBuilder.New();
                    ifProfile.AddIfElse(currentIf);
                }

                currentIf
                    .SetCondition($"{_profile} == {enumName}.{profile.Name}")
                    .AddCode(
                        MethodCallBuilder
                            .New()
                            .SetMethodName("ConfigureClient" + profile.Name)
                            .AddArgument(_sp)
                            .AddArgument(_serviceCollection)
                            .AddArgument(_strategy));
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
                        .SetMethodName(TypeNames.BuildServiceProvider)
                        .AddArgument(_serviceCollection)));
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
                        .SetLefthandSide($"var {_serviceCollection}")
                        .SetRighthandSide(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.ServiceCollection)))
                .AddMethodCall(x => x
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddArgument(_services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .SetBlock(true)
                        .AddArgument(_sp)
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
                        TypeNames.ClientBuilder.WithGeneric(descriptor.StoreAccessor.RuntimeType))
                    .AddArgument(descriptor.Name.AsStringToken())
                    .AddArgument(_services)
                    .AddArgument(_serviceCollection));

        private static ICode RegisterSerializerResolver() =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddGeneric(TypeNames.ISerializerResolver)
                .AddArgument(_services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetCode(
                        MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.SerializerResolver)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.Concat)
                                .AddArgument(
                                    MethodCallBuilder
                                        .Inline()
                                        .SetMethodName(TypeNames.GetRequiredService)
                                        .SetWrapArguments()
                                        .AddGeneric(
                                            TypeNames.IEnumerable.WithGeneric(
                                                TypeNames.ISerializer))
                                        .AddArgument(_parentServices))
                                .AddArgument(MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(TypeNames.GetRequiredService)
                                    .SetWrapArguments()
                                    .AddGeneric(
                                        TypeNames.IEnumerable.WithGeneric(TypeNames.ISerializer))
                                    .AddArgument(_sp)))));

        private static ICode RegisterStoreAccessor(
            CSharpSyntaxGeneratorSettings settings,
            StoreAccessorDescriptor storeAccessor)
        {
            if (settings.IsStoreDisabled())
            {
                return MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddArgument(_services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(storeAccessor.RuntimeType.ToString())));
            }

            return MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddArgument(_services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(storeAccessor.RuntimeType.ToString())
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric("ClientServiceProvider")
                                .AddArgument(_sp))
                            .AddGeneric(TypeNames.IOperationStore))
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric("ClientServiceProvider")
                                .AddArgument(_sp))
                            .AddGeneric(TypeNames.IEntityStore))
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric("ClientServiceProvider")
                                .AddArgument(_sp))
                            .AddGeneric(TypeNames.IEntityIdSerializer))
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric("ClientServiceProvider")
                                .AddArgument(_sp))
                            .AddGeneric(
                                TypeNames.IEnumerable.WithGeneric(
                                    TypeNames.IOperationRequestFactory)))
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric("ClientServiceProvider")
                                .AddArgument(_sp))
                            .AddGeneric(
                                TypeNames.IEnumerable.WithGeneric(
                                    TypeNames.IOperationResultDataFactory)))));
        }

        private static ICode ForwardSingletonToClientServiceProvider(string generic) =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddArgument(_services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetCode(MethodCallBuilder
                        .Inline()
                        .SetMethodName(TypeNames.GetRequiredService)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument(_sp))
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

            CodeBlockBuilder body = CodeBlockBuilder
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

            foreach (var typeDescriptor in descriptor.TypeDescriptors
                .OfType<INamedTypeDescriptor>())
            {
                if (typeDescriptor.Kind == TypeKind.Entity && !typeDescriptor.IsInterface())
                {
                    INamedTypeDescriptor namedTypeDescriptor =
                        (INamedTypeDescriptor)typeDescriptor.NamedType();
                    NameString className = namedTypeDescriptor.ExtractMapperName();

                    var interfaceName =
                        TypeNames.IEntityMapper.WithGeneric(
                            namedTypeDescriptor.ExtractType().ToString(),
                            $"{rootNamespace}.{typeDescriptor.RuntimeType.Name}");

                    body.AddMethodCall()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(interfaceName)
                        .AddGeneric($"{CreateStateNamespace(rootNamespace)}.{className}")
                        .AddArgument(_services);
                }
            }

            body.AddEmptyLine();

            foreach (var enumType in descriptor.EnumTypeDescriptor)
            {
                body.AddMethodCall()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(TypeNames.ISerializer)
                    .AddGeneric(CreateEnumParserName($"{rootNamespace}.{enumType.Name}"))
                    .AddArgument(_services);
            }

            foreach (var serializer in _serializers)
            {
                body.AddMethodCall()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(TypeNames.ISerializer)
                    .AddGeneric(serializer)
                    .AddArgument(_services);
            }

            RuntimeTypeInfo stringTypeInfo = new RuntimeTypeInfo(TypeNames.String);
            foreach (var scalar in descriptor.TypeDescriptors.OfType<ScalarTypeDescriptor>())
            {
                if (scalar.RuntimeType.Equals(stringTypeInfo) &&
                    scalar.SerializationType.Equals(stringTypeInfo) &&
                    !BuiltInScalarNames.IsBuiltInScalar(scalar.Name))
                {
                    body.AddMethodCall()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.ISerializer)
                        .AddArgument(_services)
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.StringSerializer)
                            .AddArgument(scalar.Name.AsStringToken()));
                }
            }

            foreach (var inputTypeDescriptor in descriptor.TypeDescriptors
                .Where(x => x.Kind is TypeKind.Input))
            {
                var formatter =
                    CreateInputValueFormatter(
                        (InputObjectTypeDescriptor)inputTypeDescriptor.NamedType());

                body.AddMethodCall()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(TypeNames.ISerializer)
                    .AddGeneric($"{rootNamespace}.{formatter}")
                    .AddArgument(_services);
            }

            body.AddCode(RegisterSerializerResolver());

            body.AddEmptyLine();

            foreach (var operation in descriptor.Operations)
            {
                if (!(operation.ResultTypeReference is InterfaceTypeDescriptor typeDescriptor))
                {
                    continue;
                }

                TransportType operationKind = operation switch
                {
                    SubscriptionOperationDescriptor => profile.Subscription,
                    QueryOperationDescriptor => profile.Query,
                    MutationOperationDescriptor => profile.Mutation,
                    _ => throw ThrowHelper.DependencyInjection_InvalidOperationKind(operation)
                };

                string connectionKind = operationKind switch
                {
                    TransportType.Http => TypeNames.IHttpConnection,
                    TransportType.WebSocket => TypeNames.IWebSocketConnection,
                    TransportType.InMemory => TypeNames.IInMemoryConnection,
                    { } v => throw ThrowHelper.DependencyInjection_InvalidTransportType(v)
                };

                string operationName = operation.Name;
                string fullName = operation.RuntimeType.ToString();
                string operationInterfaceName = operation.InterfaceType.ToString();
                string resultInterface = typeDescriptor.RuntimeType.ToString();

                // The factories are generated based on the concrete result type, which is the
                // only implementee of the result type interface.

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
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.IEntityIdSerializer)
                        .AddGeneric(descriptor.EntityIdFactoryDescriptor.Type.ToString())
                        .AddArgument(_services));
            }

            body.AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(descriptor.ClientDescriptor.RuntimeType.ToString())
                    .AddArgument(_services));

            body.AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(descriptor.ClientDescriptor.InterfaceType.ToString())
                    .AddArgument(_services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .AddGeneric(descriptor.ClientDescriptor.RuntimeType.ToString())
                            .AddArgument(_sp))));

            body.AddLine($"return {_services};");

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
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(
                            TypeNames.IOperationResultDataFactory.WithGeneric(resultInterface))
                        .AddGeneric(factory)
                        .AddArgument(_services))
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.IOperationResultDataFactory)
                        .AddArgument(_services)
                        .AddArgument(LambdaBuilder
                            .New()
                            .AddArgument(_sp)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(
                                    TypeNames.IOperationResultDataFactory
                                        .WithGeneric(resultInterface))
                                .AddArgument(_sp))))
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.IOperationRequestFactory)
                        .AddArgument(_services)
                        .AddArgument(LambdaBuilder
                            .New()
                            .AddArgument(_sp)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(operationInterfaceName)
                                .AddArgument(_sp))))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(
                        TypeNames.IOperationResultBuilder
                            .WithGeneric(TypeNames.JsonDocument, resultInterface))
                    .AddGeneric(resultBuilder)
                    .AddArgument(_services))
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.IOperationExecutor.WithGeneric(resultInterface))
                        .AddArgument(_services)
                        .AddArgument(LambdaBuilder
                            .New()
                            .AddArgument(_sp)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetNew()
                                .SetMethodName(settings.IsStoreEnabled()
                                    ? TypeNames.OperationExecutor
                                    : TypeNames.StorelessOperationExecutor)
                                .AddGeneric(TypeNames.JsonDocument)
                                .AddGeneric(resultInterface)
                                .AddArgument(
                                    MethodCallBuilder
                                        .Inline()
                                        .SetMethodName(TypeNames.GetRequiredService)
                                        .AddGeneric(connectionKind)
                                        .AddArgument(_sp))
                                .AddArgument(
                                    LambdaBuilder
                                        .New()
                                        .SetCode(
                                            MethodCallBuilder
                                                .Inline()
                                                .SetMethodName(
                                                    TypeNames.GetRequiredService)
                                                .AddGeneric(
                                                    TypeNames.IOperationResultBuilder.WithGeneric(
                                                        TypeNames.JsonDocument,
                                                        resultInterface))
                                                .AddArgument(_sp)))
                                .If(settings.IsStoreEnabled(),
                                    x => x
                                        .AddArgument(
                                            MethodCallBuilder
                                                .Inline()
                                                .SetMethodName(TypeNames.GetRequiredService)
                                                .AddGeneric(TypeNames.IOperationStore)
                                                .AddArgument(_sp))
                                        .AddArgument(_strategy)))))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(operationFullName)
                    .AddArgument(_services))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(operationInterfaceName)
                    .AddArgument(_services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .AddGeneric(operationFullName)
                            .AddArgument(_sp))));
        }

        private static ICode RegisterHttpConnection(string clientName) =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddArgument(_services)
                .AddGeneric(TypeNames.IHttpConnection)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetBlock(true)
                    .SetCode(CodeBlockBuilder
                        .New()
                        .AddCode(AssignmentBuilder
                            .New()
                            .SetLefthandSide($"var {_clientFactory}")
                            .SetRighthandSide(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(TypeNames.IHttpClientFactory)
                                .AddArgument(_parentServices)))
                        .AddCode(MethodCallBuilder
                            .New()
                            .SetReturn()
                            .SetNew()
                            .SetMethodName(TypeNames.HttpConnection)
                            .AddArgument(LambdaBuilder
                                .New()
                                .SetCode(MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(
                                        _clientFactory,
                                        nameof(IHttpClientFactory.CreateClient))
                                    .AddArgument(clientName.AsStringToken()))))));

        private static ICode RegisterConnection(TransportType transportProfile, string clientName)
        {
            return transportProfile switch
            {
                TransportType.WebSocket => RegisterWebSocketConnection(clientName),
                TransportType.Http => RegisterHttpConnection(clientName),
                TransportType.InMemory => RegisterInMemoryConnection(clientName),
                { } v => throw ThrowHelper.DependencyInjection_InvalidTransportType(v)
            };
        }

        private static ICode RegisterInMemoryConnection(string clientName)
        {
            return MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddGeneric(TypeNames.IInMemoryConnection)
                .AddArgument(_services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetBlock(true)
                    .SetCode(CodeBlockBuilder
                        .New()
                        .AddCode(AssignmentBuilder
                            .New()
                            .SetLefthandSide($"var {_clientFactory}")
                            .SetRighthandSide(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(TypeNames.IInMemoryClientFactory)
                                .AddArgument(_parentServices)))
                        .AddCode(MethodCallBuilder
                            .New()
                            .SetReturn()
                            .SetNew()
                            .SetMethodName(TypeNames.InMemoryConnection)
                            .AddArgument(LambdaBuilder
                                .New()
                                .SetAsync()
                                .AddArgument(_ct)
                                .SetCode(MethodCallBuilder
                                    .Inline()
                                    .SetAwait()
                                    .SetMethodName(_clientFactory, "CreateAsync")
                                    .AddArgument(clientName.AsStringToken())
                                    .AddArgument(_ct))))));
        }

        private static ICode RegisterWebSocketConnection(string clientName) =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddGeneric(TypeNames.IWebSocketConnection)
                .AddArgument(_services)
                .AddArgument(LambdaBuilder
                    .New()
                    .AddArgument(_sp)
                    .SetBlock(true)
                    .SetCode(CodeBlockBuilder
                        .New()
                        .AddCode(AssignmentBuilder
                            .New()
                            .SetLefthandSide($"var {_sessionPool}")
                            .SetRighthandSide(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(TypeNames.ISessionPool)
                                .AddArgument(_parentServices)))
                        .AddCode(MethodCallBuilder
                            .New()
                            .SetReturn()
                            .SetNew()
                            .SetMethodName(TypeNames.WebSocketConnection)
                            .AddArgument(LambdaBuilder
                                .New()
                                .SetAsync()
                                .AddArgument(_ct)
                                .SetCode(MethodCallBuilder
                                    .Inline()
                                    .SetAwait()
                                    .SetMethodName(_sessionPool, "CreateAsync")
                                    .AddArgument(clientName.AsStringToken())
                                    .AddArgument(_ct))))));

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
                    .SetMethodName(TypeNames.TryAddSingleton)
                    .AddGeneric(TypeNames.IEntityStore)
                    .AddGeneric(TypeNames.EntityStore)
                    .AddArgument(_services))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.TryAddSingleton)
                    .AddGeneric(TypeNames.IOperationStore)
                    .AddArgument(_services)
                    .AddArgument(LambdaBuilder
                        .New()
                        .AddArgument(_sp)
                        .SetCode(MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(TypeNames.OperationStore)
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(TypeNames.GetRequiredService)
                                .AddGeneric(TypeNames.IEntityStore)
                                .AddArgument(_sp)))));
        }

        private static string _clientServiceProvider = @"
        private class ClientServiceProvider
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
        }
";
    }
}
