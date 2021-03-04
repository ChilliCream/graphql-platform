using System.Linq;
using System.Net.Http;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class DependencyInjectionGenerator : CodeGenerator<DependencyInjectionDescriptor>
    {
        private const string _sessionPool = "sessionPool";
        private const string _services = "services";
        private const string _strategy = "strategy";
        private const string _parentServices = "parentServices";
        private const string _clientFactory = "clientFactory";
        private const string _serviceCollection = "serviceCollection";
        private const string _sp = "sp";

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
            CodeWriter writer,
            DependencyInjectionDescriptor descriptor,
            out string fileName)
        {
            fileName = CreateServiceCollectionExtensions(descriptor.Name);

            ClassBuilder factory = ClassBuilder
                .New(fileName)
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public);

            factory
                .AddMethod($"Add{descriptor.Name}")
                .SetPublic()
                .SetStatic()
                .SetReturnType(TypeNames.IServiceCollection)
                .AddParameter(
                    _services,
                    x => x.SetThis().SetType(TypeNames.IServiceCollection))
                .AddParameter(
                    _strategy,
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(
                            TypeNames.ExecutionStrategy + "." +
                            "NetworkOnly"))
                .AddCode(GenerateMethodBody(descriptor));

            factory
                .AddMethod("ConfigureClient")
                .SetPrivate()
                .SetStatic()
                .SetReturnType(TypeNames.IServiceCollection)
                .AddParameter(_services, x => x.SetType(TypeNames.IServiceCollection))
                .AddParameter(_parentServices, x => x.SetType(TypeNames.IServiceProvider))
                .AddParameter(
                    _strategy,
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(
                            TypeNames.ExecutionStrategy + "." +
                            "NetworkOnly"))
                .AddCode(GenerateInternalMethodBody(descriptor));

            factory.AddClass(_clientServiceProvider);

            CodeFileBuilder
                .New()
                .SetNamespace(TypeNames.DependencyInjectionNamespace)
                .AddType(factory)
                .Build(writer);
        }

        private static ICode GenerateMethodBody(DependencyInjectionDescriptor descriptor) =>
            CodeBlockBuilder
                .New()
                .AddMethodCall(x =>
                    x.SetMethodName(TypeNames.AddSingleton)
                        .AddArgument(_services)
                        .AddArgument(LambdaBuilder
                            .New()
                            .SetBlock(true)
                            .AddArgument(_sp)
                            .SetCode(
                                CodeBlockBuilder
                                    .New()
                                    .AddCode(
                                        AssignmentBuilder
                                            .New()
                                            .SetLefthandSide($"var {_serviceCollection}")
                                            .SetRighthandSide(
                                                MethodCallBuilder
                                                    .Inline()
                                                    .SetNew()
                                                    .SetMethodName(TypeNames.ServiceCollection)))
                                    .AddEmptyLine()
                                    .AddMethodCall(x => x.SetMethodName("ConfigureClient")
                                        .AddArgument(_serviceCollection)
                                        .AddArgument(_sp)
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
                                            .AddArgument(_serviceCollection))))))
                .AddEmptyLine()
                .ForEach(
                    descriptor.Operations,
                    (builder, operation) =>
                        builder.AddCode(ForwardSingletonToClientServiceProvider(
                            operation.RuntimeType.ToString())))
                .AddEmptyLine()
                .AddCode(ForwardSingletonToClientServiceProvider(
                    $"{descriptor.RuntimeType.Namespace}.{descriptor.Name}"))
                .AddEmptyLine()
                .AddLine($"return {_services};");

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

        private ICode GenerateInternalMethodBody(DependencyInjectionDescriptor descriptor)
        {
            var rootNamespace = descriptor.RuntimeType.Namespace;

            bool hasSubscriptions =
                descriptor.Operations.OfType<SubscriptionOperationDescriptor>().Any();
            bool hasQueries =
                descriptor.Operations.OfType<QueryOperationDescriptor>().Any();
            bool hasMutations =
                descriptor.Operations.OfType<MutationOperationDescriptor>().Any();

            var body = CodeBlockBuilder
                .New()
                .AddCode(CreateBaseCode(descriptor.RuntimeType.Namespace));

            if (hasSubscriptions)
            {
                body.AddCode(
                    RegisterWebSocketConnection($"{rootNamespace}.{descriptor.Name}"));
            }

            if (hasQueries || hasMutations)
            {
                body.AddCode(RegisterHttpConnection(descriptor.Name));
            }

            body.AddEmptyLine();

            foreach (var typeDescriptor in descriptor.TypeDescriptors
                .OfType<INamedTypeDescriptor>())
            {
                if (typeDescriptor.Kind == TypeKind.EntityType && !typeDescriptor.IsInterface())
                {
                    INamedTypeDescriptor namedTypeDescriptor =
                        (INamedTypeDescriptor)typeDescriptor.NamedType();
                    NameString className = namedTypeDescriptor.ExtractMapperName();

                    var interfaceName =
                        TypeNames.IEntityMapper.WithGeneric(
                            $"{rootNamespace}.{namedTypeDescriptor.ExtractTypeName()}",
                            $"{rootNamespace}.{typeDescriptor.RuntimeType.Name}"
                        );

                    body.AddMethodCall()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(interfaceName)
                        .AddGeneric($"{rootNamespace}.{className}")
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

            RuntimeTypeInfo stringTypeInfo = TypeInfos.From(TypeNames.String);
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
                .Where(x => x.Kind is TypeKind.InputType))
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

                string connectionKind = operation is SubscriptionOperationDescriptor
                    ? TypeNames.WebSocketConnection
                    : TypeNames.HttpConnection;
                string operationName = operation.Name;
                string fullName = operation.RuntimeType.ToString();
                string operationInterface = typeDescriptor.RuntimeType.ToString();

                // The factories are generated based on the concrete result type, which is the
                // only implementee of the result type interface.

                var factoryName =
                    CreateResultFactoryName(
                        typeDescriptor.ImplementedBy.First().RuntimeType.Name);

                var builderName = CreateResultBuilderName(operationName);
                body.AddCode(
                    RegisterOperation(
                        connectionKind,
                        fullName,
                        operationInterface,
                        $"{operation.RuntimeType.Namespace}.{factoryName}",
                        $"{operation.RuntimeType.Namespace}.{builderName}"));
            }

            body.AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(descriptor.RuntimeType.ToString())
                    .AddArgument(_services));

            body.AddLine($"return {_services};");

            return body;
        }

        private static ICode RegisterOperation(
            string connectionKind,
            string fullName,
            string operationInterface,
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
                            TypeNames.IOperationResultDataFactory.WithGeneric(operationInterface))
                        .AddGeneric(factory)
                        .AddArgument(_services))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(
                        TypeNames.IOperationResultBuilder
                            .WithGeneric(TypeNames.JsonDocument, operationInterface))
                    .AddGeneric(resultBuilder)
                    .AddArgument(_services))
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(TypeNames.AddSingleton)
                        .AddGeneric(TypeNames.IOperationExecutor.WithGeneric(operationInterface))
                        .AddArgument(_services)
                        .AddArgument(LambdaBuilder
                            .New()
                            .AddArgument(_sp)
                            .SetCode(MethodCallBuilder
                                .Inline()
                                .SetNew()
                                .SetMethodName(TypeNames.OperationExecutor)
                                .AddGeneric(TypeNames.JsonDocument)
                                .AddGeneric(operationInterface)
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
                                                        operationInterface))
                                                .AddArgument(_sp)))
                                .AddArgument(
                                    MethodCallBuilder
                                        .Inline()
                                        .SetMethodName(TypeNames.GetRequiredService)
                                        .AddGeneric(TypeNames.IOperationStore)
                                        .AddArgument(_sp))
                                .AddArgument(_strategy))))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(fullName)
                    .AddArgument(_services));
        }

        private static ICode RegisterHttpConnection(string clientName) =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
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


        private static ICode RegisterWebSocketConnection(string clientName) =>
            MethodCallBuilder
                .New()
                .SetMethodName(TypeNames.AddSingleton)
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
                                .SetCode(MethodCallBuilder
                                    .Inline()
                                    .SetMethodName(_sessionPool, "CreateAsync")
                                    .AddArgument(clientName.AsStringToken())
                                    .AddArgument("default"))))));

        private static ICode CreateBaseCode(string @namespace) =>
            CodeBlockBuilder
                .New()
                .AddCode(IfBuilder
                    .New()
                    .SetCondition($"{_services} is null")
                    .AddCode(ExceptionBuilder
                        .New(TypeNames.ArgumentNullException)
                        .AddArgument($"nameof({_services})")))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetMethodName(TypeNames.AddSingleton)
                    .AddGeneric(TypeNames.Func.WithGeneric(
                        TypeNames.JsonElement,
                        TypeNames.EntityId))
                    .AddArgument(_services)
                    .AddArgument($"{@namespace}.EntityIdFactory.CreateEntityId"))
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
                                .AddArgument(_sp)
                                .Chain(x => x.SetMethodName("Watch"))))));

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
