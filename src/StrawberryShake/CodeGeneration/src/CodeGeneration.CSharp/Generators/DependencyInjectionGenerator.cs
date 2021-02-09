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
        private static string[] _serializers = new[]
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
            TypeNames.IDSerializer,
            TypeNames.DateTimeSerializer,
            TypeNames.DateSerializer,
            TypeNames.ByteArraySerializer,
            TypeNames.TimeSpanSerializer
        };

        private static string[] _websocketProtocols = new[]
        {
            TypeNames.GraphQLWebSocketProtocolFactory
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

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(factory)
                .Build(writer);
        }

        private ICode GenerateMethodBody(DependencyInjectionDescriptor descriptor)
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
                    EnumParserNameFromEnumName(enumType.Name));
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

            AddSingleton(codeWriter, TypeNames.ISerializerResolver, TypeNames.SerializerResolver);

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
                var factory = ResultFactoryNameFromTypeName(operationName);
                var resultBuilder = ResultBuilderNameFromTypeName(operationName);
                stringBuilder.AppendLine(
                    RegisterOperation(
                        connectionKind,
                        descriptor.Name,
                        operationName,
                        fullName,
                        operationInterface,
                        factory,
                        resultBuilder));
            }

            if (hasSubscriptions)
            {
                codeWriter.WriteLine();
                codeWriter.WriteComment("register websocket protocols");

                foreach (var protocol in _websocketProtocols)
                {
                    AddProtocol(codeWriter, protocol);
                }
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("return services;");

            return CodeBlockBuilder.From(stringBuilder);
        }

        private static string RegisterOperation(
            string connectionKind,
            string clientName,
            string operationName,
            string fullName,
            string operationInterface,
            string factory,
            string resultBuilder) => $@"
{TypeNames.AddSingleton}<
    {TypeNames.IOperationResultDataFactory.WithGeneric(operationName)},
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
{TypeNames.AddSingleton.WithGeneric(clientName)}(services);";

        private static string RegisterHttpConnection(string clientName) => $@"
{TypeNames.AddSingleton}(
    services,
    sp =>
    {{
        var clientFactory =
            {TypeNames.GetRequiredService}<
                {TypeNames.IHttpClientFactory}
                >(sp);

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
                >(sp);

        return new {TypeNames.WebSocketConnection}(
            () => sessionPool.CreateAsync(""{clientName}"", default));
    }});
";

        private static string _staticCode = $@"
if (services is null)
{{
    throw new {TypeNames.ArgumentNullException}(nameof(services));
}}

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

        private void AddSingleton(
            CodeWriter writer,
            string @interface,
            string type)
        {
            writer.WriteLine(TypeNames.AddSingleton.WithGeneric(@interface, type) + "(services);");
        }

        private void AddProtocol(
            CodeWriter writer,
            string protocol)
        {
            writer.WriteLine(TypeNames.AddProtocol.WithGeneric(protocol) + "(services);");
        }
    }
}
