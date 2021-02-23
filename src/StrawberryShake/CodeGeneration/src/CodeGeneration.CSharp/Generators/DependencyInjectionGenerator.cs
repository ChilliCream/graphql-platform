using System;
using System.Linq;
using System.Text;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DependencyInjectionGenerator : CodeGenerator<DependencyInjectionDescriptor>
    {
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
            fileName = ServiceCollectionExtensionsFromClientName(descriptor.Name);

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
                    "services",
                    x => x.SetThis().SetType(TypeNames.IServiceCollection))
                .AddParameter(
                    "strategy",
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(
                            TypeNames.ExecutionStrategy + "." +
                            nameof(ExecutionStrategy.NetworkOnly)))
                .AddCode(GenerateMethodBody(descriptor));

            factory
                .AddMethod("ConfigureClient")
                .SetPrivate()
                .SetStatic()
                .SetReturnType(TypeNames.IServiceCollection)
                .AddParameter("services", x => x.SetType(TypeNames.IServiceCollection))
                .AddParameter("parentServices", x => x.SetType(TypeNames.IServiceProvider))
                .AddParameter(
                    "strategy",
                    x => x.SetType(TypeNames.ExecutionStrategy)
                        .SetDefault(
                            TypeNames.ExecutionStrategy + "." +
                            nameof(ExecutionStrategy.NetworkOnly)))
                .AddCode(GenerateInternalMethodBody(descriptor));

            factory.AddClass(_clientServiceProvider);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(factory)
                .Build(writer);
        }

        private static ICode GenerateMethodBody(DependencyInjectionDescriptor descriptor) =>
            CodeBlockBuilder.New()
                .AddMethodCall(x =>
                    x.SetMethodName(TypeNames.AddSingleton)
                        .AddArgument("services")
                        .AddArgument(LambdaBuilder.New()
                            .SetBlock(true)
                            .AddArgument("sp")
                            .SetCode(
                                CodeBlockBuilder.New()
                                    .AddCode(
                                        AssignmentBuilder.New()
                                            .SetLefthandSide("var serviceCollection")
                                            .SetRighthandSide(
                                                $"new {TypeNames.ServiceCollection}()"))
                                    .AddEmptyLine()
                                    .AddMethodCall(x => x.SetMethodName("ConfigureClient")
                                        .AddArgument("serviceCollection")
                                        .AddArgument("sp")
                                        .AddArgument("strategy"))
                                    .AddEmptyLine()
                                    .AddCode(MethodCallBuilder.New()
                                        .SetPrefix("return new ")
                                        .SetWrapArguments()
                                        .SetMethodName("ClientServiceProvider")
                                        .AddArgument(MethodCallBuilder.New()
                                            .SetMethodName(TypeNames.BuildServiceProvider)
                                            .SetDetermineStatement(false)
                                            .AddArgument("serviceCollection"))))))
                .AddEmptyLine()
                .ForEach(
                    descriptor.Operations,
                    (builder, operation) =>
                        builder.AddCode(ForwardSingletonToClientServiceProvider(operation.Name)))
                .AddEmptyLine()
                .AddCode(ForwardSingletonToClientServiceProvider(descriptor.Name))
                .AddEmptyLine()
                .AddLine("return services;");

        private static ICode RegisterSerializerResolver() =>
            MethodCallBuilder.New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddGeneric(TypeNames.ISerializerResolver)
                .AddArgument("services")
                .AddArgument(LambdaBuilder.New()
                    .AddArgument("sp")
                    .SetCode(
                        MethodCallBuilder.New()
                            .SetPrefix("new ")
                            .SetMethodName(TypeNames.SerializerResolver)
                            .SetDetermineStatement(false)
                            .SetWrapArguments()
                            .AddArgument(MethodCallBuilder.New()
                                .SetMethodName(TypeNames.Concat)
                                .SetDetermineStatement(false)
                                .AddArgument(
                                    MethodCallBuilder.New()
                                        .SetMethodName(TypeNames.GetRequiredService)
                                        .SetDetermineStatement(false)
                                        .SetWrapArguments()
                                        .AddGeneric(
                                            TypeNames.IEnumerable.WithGeneric(TypeNames
                                                .ISerializer))
                                        .AddArgument("parentServices"))
                                .AddArgument(MethodCallBuilder.New()
                                    .SetMethodName(TypeNames.GetRequiredService)
                                    .SetDetermineStatement(false)
                                    .SetWrapArguments()
                                    .AddGeneric(
                                        TypeNames.IEnumerable.WithGeneric(TypeNames.ISerializer))
                                    .AddArgument("sp")))));

        private static ICode ForwardSingletonToClientServiceProvider(string generic) =>
            MethodCallBuilder.New()
                .SetMethodName(TypeNames.AddSingleton)
                .AddArgument("services")
                .AddArgument(LambdaBuilder.New()
                    .AddArgument("sp")
                    .SetCode(MethodCallBuilder.New()
                        .SetMethodName(TypeNames.GetRequiredService)
                        .SetDetermineStatement(false)
                        .SetWrapArguments()
                        .AddArgument(MethodCallBuilder.New()
                            .SetMethodName(TypeNames.GetRequiredService)
                            .SetDetermineStatement(false)
                            .AddGeneric("ClientServiceProvider")
                            .AddArgument("sp"))
                        .AddGeneric(generic)));

        private ICode GenerateInternalMethodBody(DependencyInjectionDescriptor descriptor)
        {
            bool hasSubscriptions =
                descriptor.Operations.OfType<SubscriptionOperationDescriptor>().Any();
            bool hasQueries =
                descriptor.Operations.OfType<QueryOperationDescriptor>().Any();
            bool hasMutations =
                descriptor.Operations.OfType<MutationOperationDescriptor>().Any();

            var stringBuilder = new StringBuilder();
            var codeWriter = new CodeWriter(stringBuilder);

            stringBuilder.AppendLine(_staticCode);

            codeWriter.WriteComment("register connections");

            if (hasSubscriptions)
            {
                stringBuilder.AppendLine(RegisterWebSocketConnection(descriptor.Name));
            }

            if (hasQueries || hasMutations)
            {
                stringBuilder.AppendLine(RegisterHttpConnection(descriptor.Name));
            }

            codeWriter.WriteComment("register mappers");
            codeWriter.WriteLine();

            foreach (var typeDescriptor in descriptor.TypeDescriptors)
            {
                if (typeDescriptor.Kind == TypeKind.EntityType && !typeDescriptor.IsInterface())
                {
                    NamedTypeDescriptor namedTypeDescriptor =
                        (NamedTypeDescriptor)typeDescriptor.NamedType();
                    NameString className = namedTypeDescriptor.ExtractMapperName();

                    var interfaceName =
                        TypeNames.IEntityMapper.WithGeneric(
                            namedTypeDescriptor.ExtractTypeName(),
                            typeDescriptor.Name);

                    AddSingleton(codeWriter, interfaceName, className);
                }
            }

            codeWriter.WriteLine();
            codeWriter.WriteComment("register serializers");
            codeWriter.WriteLine();

            foreach (var enumType in descriptor.EnumTypeDescriptor)
            {
                AddSingleton(
                    codeWriter,
                    TypeNames.ISerializer,
                    CreateEnumParserName(enumType.Name));
            }

            foreach (var serializer in _serializers)
            {
                AddSingleton(
                    codeWriter,
                    TypeNames.ISerializer,
                    serializer);
            }

            foreach (var inputTypeDescriptor in descriptor.TypeDescriptors
                .Where(x => x.Kind is TypeKind.InputType))
            {
                AddSingleton(
                    codeWriter,
                    TypeNames.ISerializer,
                    InputValueFormatterFromType(
                        (NamedTypeDescriptor)inputTypeDescriptor.NamedType()));
            }

            RegisterSerializerResolver().Build(codeWriter);

            codeWriter.WriteLine();
            codeWriter.WriteComment("register operations");
            foreach (var operation in descriptor.Operations)
            {
                string connectionKind = operation is SubscriptionOperationDescriptor
                    ? TypeNames.WebSocketConnection
                    : TypeNames.HttpConnection;
                NameString operationName = operation.OperationName;
                NameString fullName = operation.Name;
                NameString operationInterface = operation.ResultTypeReference.Name;

                // The resulttype of the operation is a NamedTypeDescriptor, that is an Interface
                var resultType = operation.ResultTypeReference as NamedTypeDescriptor
                                         ?? throw new ArgumentException("ResultTypeReference");
                // The factories are generated based on the concrete result type, which is the
                // only implementee of the result type interface.
                var factoryName = ResultFactoryNameFromTypeName(resultType.ImplementedBy[0].Name);

                var builderName = ResultBuilderNameFromTypeName(operationName);
                stringBuilder.AppendLine(
                    RegisterOperation(
                        connectionKind,
                        descriptor.Name,
                        fullName,
                        operationInterface,
                        factoryName,
                        builderName));
            }

            stringBuilder.AppendLine(
                $"{TypeNames.AddSingleton.WithGeneric(descriptor.Name)}(services);");

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("return services;");

            return CodeBlockBuilder.From(stringBuilder);
        }

        private void AddSingleton(
            CodeWriter writer,
            string @interface,
            string type)
        {
            writer.WriteLine(TypeNames.AddSingleton.WithGeneric(@interface, type) +
                "(services);");
        }

        private void AddProtocol(
            CodeWriter writer,
            string protocol)
        {
            writer.WriteLine(TypeNames.AddProtocol.WithGeneric(protocol) + "(services);");
        }

        // TODO : Lets clean this up.
        private static string RegisterOperation(
            string connectionKind,
            string clientName,
            string fullName,
            string operationInterface,
            string factory,
            string resultBuilder) => $@"
{TypeNames.AddSingleton}<
    {TypeNames.IOperationResultDataFactory.WithGeneric(operationInterface)},
    {factory}>(
        services);
{TypeNames.AddSingleton}<
    {TypeNames.IOperationResultBuilder.WithGeneric(TypeNames.JsonDocument, operationInterface)},
    {resultBuilder}>(
        services);
{TypeNames.AddSingleton}<
    {TypeNames.IOperationExecutor.WithGeneric(operationInterface)}>(
        services,
        sp => new {TypeNames.OperationExecutor.WithGeneric(TypeNames.JsonDocument, operationInterface)}(
            {TypeNames.GetRequiredService.WithGeneric(connectionKind)}(sp),
            () => {TypeNames.GetRequiredService.WithGeneric(TypeNames.IOperationResultBuilder.WithGeneric(TypeNames.JsonDocument, operationInterface))}(sp),
            {TypeNames.GetRequiredService.WithGeneric(TypeNames.IOperationStore)}(sp),
            strategy));

{TypeNames.AddSingleton.WithGeneric(fullName)}(services);
";

        private static string RegisterHttpConnection(string clientName) => $@"
{TypeNames.AddSingleton}(
    services,
    sp =>
    {{
        var clientFactory =
            {TypeNames.GetRequiredService}<
                {TypeNames.IHttpClientFactory}
                >(parentServices);

        return new {TypeNames.HttpConnection}(
            () => clientFactory.CreateClient(""{clientName}""));
    }});
";

        private static string RegisterWebSocketConnection(string clientName) => $@"
{TypeNames.AddSingleton}(
    services,
    sp =>
    {{
        var sessionPool =
            {TypeNames.GetRequiredService}<
                {TypeNames.ISessionPool}
                >(parentServices);

        return new {TypeNames.WebSocketConnection}(
            () => sessionPool.CreateAsync(""{clientName}"", default));
    }});
";

        private static string _staticCode = $@"
if (services is null)
{{
    throw new {TypeNames.ArgumentNullException}(nameof(services));
}}

// register entity id factory

{TypeNames.AddSingleton.WithGeneric(TypeNames.Func.WithGeneric(TypeNames.JsonElement, TypeNames.EntityId))}(services, EntityIdFactory.CreateEntityId);

// register stores

{TypeNames.TryAddSingleton}<
    {TypeNames.IEntityStore},
    {TypeNames.EntityStore}>(
        services);
{TypeNames.TryAddSingleton}<
    {TypeNames.IOperationStore}>(
        services,
        sp => new {TypeNames.OperationStore}(
            {TypeNames.GetRequiredService}<
                {TypeNames.IEntityStore}
                >(sp)
            .Watch()
            ));
";
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
