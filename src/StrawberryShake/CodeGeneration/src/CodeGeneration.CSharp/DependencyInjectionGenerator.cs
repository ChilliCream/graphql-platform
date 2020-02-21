using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DependencyInjectionGenerator
        : CodeGenerator<DependencyInjectionDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            DependencyInjectionDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name)
                    .AddField(FieldBuilder.New()
                        .SetConst()
                        .SetType("string")
                        .SetName("_clientName")
                        .SetValue($"\"{descriptor.ClientName}\""));

            AddAddClientMethod(classBuilder, descriptor, CodeWriter.Indent);

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddUsing("Microsoft.Extensions.DependencyInjection")
                .AddUsing("Microsoft.Extensions.DependencyInjection.Extensions")
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private static void AddAddClientMethod(
            ClassBuilder builder,
            DependencyInjectionDescriptor descriptor,
            string indent)
        {
            builder.AddMethod(MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetStatic()
                .SetReturnType(
                    "global::StrawberryShake.Configuration.IOperationClientBuilder")
                .SetName($"Add{descriptor.ClientName}")
                .AddParameter(ParameterBuilder.New()
                    .SetType(
                        "this global::Microsoft.Extensions.DependencyInjection." +
                        "IServiceCollection")
                    .SetName("serviceCollection"))
                .AddCode(CreateAddClientBody(descriptor, indent)));
        }

        private static CodeBlockBuilder CreateAddClientBody(
            DependencyInjectionDescriptor descriptor,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine("if (serviceCollection is null)");
            body.AppendLine("{");
            body.AppendLine(
                $"{indent}throw new global::System.ArgumentNullException(" +
                "nameof(serviceCollection));");
            body.AppendLine("}");
            body.AppendLine();

            body.AppendLine(
                "serviceCollection.AddSingleton" +
                $"<{descriptor.ClientTypeName}, {descriptor.ClientInterfaceTypeName}>();");
            body.AppendLine();

            body.AppendLine(
                "serviceCollection.AddSingleton<global::StrawberryShake." +
                "IOperationExecutorFactory>(sp =>");
            body.AppendLine(
                $"{indent}new global::StrawberryShake.Http.HttpOperationExecutorFactory(");
            body.AppendLine($"{indent}{indent}_clientName,");
            body.AppendLine(
                $"{indent}{indent}sp.GetRequiredService<global::System.Net.Http." +
                "IHttpClientFactory>().CreateClient,");
            body.AppendLine(
                $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                "Configuration.IClientOptions>()" +
                ".GetOperationPipeline<global::StrawberryShake.Http." +
                "IHttpOperationContext>(_clientName),");
            body.AppendLine(
                $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                "Configuration.IClientOptions>()." +
                "GetOperationFormatter(_clientName),");
            body.AppendLine(
                $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                "Configuration.IClientOptions>()." +
                "GetResultParsers(_clientName)));");
            body.AppendLine();

            if (descriptor.EnableSubscriptions)
            {
                body.AppendLine(
                    "serviceCollection.AddSingleton<global::StrawberryShake." +
                    "IOperationStreamExecutorFactory>(sp =>");
                body.AppendLine(
                    $"{indent}new global::StrawberryShake.Http.Subscriptions." +
                    "SocketOperationStreamExecutorFactory(");
                body.AppendLine($"{indent}{indent}_clientName,");
                body.AppendLine(
                    $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                    "Transport.ISocketConnectionPool>().RentAsync,");
                body.AppendLine(
                    $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake.Http." +
                    "Subscriptions.ISubscriptionManager>(),");
                body.AppendLine(
                    $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                    "Configuration.IClientOptions>().GetOperationFormatter(_clientName),");
                body.AppendLine(
                    $"{indent}{indent}sp.GetRequiredService<global::StrawberryShake." +
                    "Configuration.IClientOptions>().GetResultParsers(_clientName)));");
                body.AppendLine();
            }

            body.AppendLine(
                "global::StrawberryShake.Configuration.IOperationClientBuilder builder = " +
                "serviceCollection.AddOperationClientOptions(_clientName)");
            AppendSerializerRegistrations(descriptor.ValueSerializers, body, indent);
            AppendParserRegistrations(descriptor.ResultParsers, body, indent);
            body.AppendLine(
                ".AddOperationFormatter(serializers => new global::StrawberryShake." +
                "Http.JsonOperationFormatter(serializers))");
            body.AppendLine(
                ".AddHttpOperationPipeline(builder => builder.UseHttpDefaultPipeline());");

            if (descriptor.EnableSubscriptions)
            {
                body.AppendLine(
                    "serviceCollection.TryAddSingleton<global::StrawberryShake.Http." +
                    "Subscriptions.ISubscriptionManager, global::StrawberryShake.Http." +
                    "Subscriptions.SubscriptionManager>();");
            }

            body.Append(
                "serviceCollection.TryAddSingleton<global::StrawberryShake." +
                "IOperationExecutorPool, global::StrawberryShake.OperationExecutorPool>();");

            if (descriptor.EnableSubscriptions)
            {
                body.AppendLine();
                body.AppendLine(
                    "serviceCollection.TryAddEnumerable(new global::Microsoft.Extensions." +
                    "DependencyInjection.ServiceDescriptor(");
                body.AppendLine(
                    $"{indent}typeof(global::StrawberryShake.Transport." +
                    "ISocketConnectionInterceptor),");
                body.AppendLine(
                    $"{indent}typeof(global::StrawberryShake.Http.Subscriptions." +
                    "MessagePipelineHandler),");
                body.AppendLine(
                    $"{indent}global::Microsoft.Extensions.DependencyInjection." +
                    "ServiceLifetime.Singleton));");
                body.AppendLine();
                body.Append("return builder;");
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private static void AppendSerializerRegistrations(
            IReadOnlyList<string> serializers,
            StringBuilder body,
            string indent)
        {
            foreach (string serializer in serializers)
            {
                body.AppendLine($"{indent}.AddValueSerializer(() => new {serializer}())");
            }
        }

        private static void AppendParserRegistrations(
            IReadOnlyList<string> parsers,
            StringBuilder body,
            string indent)
        {
            foreach (string parser in parsers)
            {
                body.AppendLine(
                    $"{indent}.AddResultParser(serializers => new {parser}(serializers))");
            }
        }
    }
}
