using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ServicesGenerator
        : CodeGenerator<IServicesDescriptor>
        , IUsesComponents
    {
        public ServicesGenerator(ClientGeneratorOptions options)
            : base(options)
        {
        }

        public IReadOnlyList<string> Components { get; } = new[]
        {
            WellKnownComponents.DI,
            WellKnownComponents.Http,
            WellKnownComponents.HttpExecutor,
            WellKnownComponents.HttpExecutorPipeline,
            WellKnownComponents.Serializer,
            WellKnownComponents.Configuration
        };

        protected override Task WriteAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor,
            ITypeLookup typeLookup) =>
            WriteStaticClassAsync(writer, descriptor.Name, async () =>
            {
                await writer.WriteIndentedLineAsync(
                    $"private const string _clientName = \"{descriptor.Client.Name}\";")
                    .ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteAddClientAsync(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            });

        private static async Task WriteAddClientAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                $"Add{descriptor.Client.Name}",
                true,
                async () =>
                {
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddSingleton<{0}, {1}>();",
                        GetInterfaceName(descriptor.Client.Name),
                        GetClassName(descriptor.Client.Name))
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddSingleton<IOperationExecutorFactory>(sp =>")
                        .ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "new HttpOperationExecutorFactory(")
                            .ConfigureAwait(false);
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "_clientName,")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "sp.GetRequiredService<IHttpClientFactory>().CreateClient,")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "sp.GetRequiredService<IClientOptions>()" +
                                ".GetOperationPipeline<IHttpOperationContext>(_clientName),")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "sp.GetRequiredService<IClientOptions>()" +
                                ".GetOperationFormatter(_clientName),")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "sp.GetRequiredService<IClientOptions>()" +
                                ".GetResultParsers(_clientName)));")
                                .ConfigureAwait(false);
                        }
                    }
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    if (descriptor.OperationTypes.Contains(OperationType.Subscription))
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.AddSingleton<IOperationStreamExecutorFactory>(sp =>")
                            .ConfigureAwait(false);

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "new SocketOperationStreamExecutorFactory(")
                                .ConfigureAwait(false);
                            using (writer.IncreaseIndent())
                            {
                                await writer.WriteIndentedLineAsync(
                                    "_clientName,")
                                    .ConfigureAwait(false);
                                await writer.WriteIndentedLineAsync(
                                    "sp.GetRequiredService<ISocketConnectionPool>().RentAsync,")
                                    .ConfigureAwait(false);
                                await writer.WriteIndentedLineAsync(
                                    "sp.GetRequiredService<ISubscriptionManager>(),")
                                    .ConfigureAwait(false);
                                await writer.WriteIndentedLineAsync(
                                    "sp.GetRequiredService<IClientOptions>()" +
                                    ".GetOperationFormatter(_clientName),")
                                    .ConfigureAwait(false);
                                await writer.WriteIndentedLineAsync(
                                    "sp.GetRequiredService<IClientOptions>()" +
                                    ".GetResultParsers(_clientName)));")
                                    .ConfigureAwait(false);
                            }
                        }

                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync(
                        "IOperationClientBuilder builder = " +
                        "serviceCollection.AddOperationClientOptions(_clientName)")
                        .ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await WriteAddEnumSerializersAsync(writer, descriptor)
                            .ConfigureAwait(false);
                        await WriteAddInputSerializersAsync(writer, descriptor)
                            .ConfigureAwait(false);
                        await WriteAddResultParsersAsync(writer, descriptor)
                            .ConfigureAwait(false);
                        await WriteAddOperationSerializerAsync(writer)
                            .ConfigureAwait(false);
                        await WriteAddHttpDefaultPipelineAsync(writer)
                            .ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync().ConfigureAwait(false);

                    if (descriptor.OperationTypes.Contains(OperationType.Subscription))
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.TryAddSingleton<ISubscriptionManager, " +
                            "SubscriptionManager>();")
                            .ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.TryAddSingleton<IOperationExecutorPool, " +
                        "OperationExecutorPool>();")
                        .ConfigureAwait(false);

                    if (descriptor.OperationTypes.Contains(OperationType.Subscription))
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.TryAddEnumerable(new ServiceDescriptor(")
                            .ConfigureAwait(false);

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                "typeof(ISocketConnectionInterceptor),")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "typeof(MessagePipelineHandler),")
                                .ConfigureAwait(false);
                            await writer.WriteIndentedLineAsync(
                                "ServiceLifetime.Singleton));")
                                .ConfigureAwait(false);
                        }
                    }
                }).ConfigureAwait(false);
        }

        private static async Task WriteAddEnumSerializersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            foreach (IEnumDescriptor enumType in descriptor.EnumTypes)
            {
                await writer.WriteIndentedLineAsync(
                    ".AddValueSerializer(() => new {0}())",
                    GetClassName(enumType.Name + "ValueSerializer"))
                    .ConfigureAwait(false);
            }
        }

        private static async Task WriteAddInputSerializersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            foreach (IInputClassDescriptor inputType in descriptor.InputTypes)
            {
                await writer.WriteIndentedLineAsync(
                    ".AddValueSerializer(() => new {0}())",
                    GetClassName(inputType.Name + "Serializer"))
                    .ConfigureAwait(false);
            }
        }

        private static async Task WriteAddResultParsersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            foreach (IResultParserDescriptor resultParser in descriptor.ResultParsers)
            {
                await writer.WriteIndentedLineAsync(
                    ".AddResultParser(serializers => new {0}(serializers))",
                    GetClassName(resultParser.Name))
                    .ConfigureAwait(false);
            }
        }

        private static async Task WriteAddOperationSerializerAsync(CodeWriter writer)
        {
            await writer.WriteIndentedLineAsync(
                ".AddOperationFormatter(serializers => " +
                "new JsonOperationFormatter(serializers))")
                .ConfigureAwait(false);
        }

        private static async Task WriteAddHttpDefaultPipelineAsync(CodeWriter writer)
        {
            await writer.WriteIndentedLineAsync(
                ".AddHttpOperationPipeline(b => b.UseHttpDefaultPipeline());")
                .ConfigureAwait(false);
        }

        private static async Task WriteMethodAsync(
            CodeWriter writer,
            string methodName,
            bool isPublic,
            Func<Task> write)
        {
            await writer.WriteIndentedLineAsync(
                "{0} static IOperationClientBuilder {1}(",
                isPublic ? "public" : "private",
                methodName)
                .ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "this IServiceCollection serviceCollection)")
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                if (isPublic)
                {
                    await writer.WriteIndentedLineAsync(
                        "if (serviceCollection is null)")
                        .ConfigureAwait(false);

                    await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new ArgumentNullException(nameof(serviceCollection));")
                            .ConfigureAwait(false);
                    }

                    await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await write().ConfigureAwait(false);

                await writer.WriteIndentedLineAsync(
                    "return builder;")
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}
