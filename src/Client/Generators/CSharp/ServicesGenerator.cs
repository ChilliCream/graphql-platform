using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ServicesGenerator
        : CodeGenerator<IServicesDescriptor>
        , IUsesComponents
    {
        public IReadOnlyList<string> Components { get; } = new[]
        {
            WellKnownComponents.DI,
            WellKnownComponents.Http,
            WellKnownComponents.HttpExecutor,
            WellKnownComponents.HttpExecutorPipeline,
            WellKnownComponents.Serializer
        };

        protected override Task WriteAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor,
            ITypeLookup typeLookup) =>
            WriteStaticClassAsync(writer, descriptor.Name, async () =>
            {
                await WriteAddClientAsync(writer, descriptor.Client);
                await writer.WriteLineAsync();

                await WriteAddSerializersAsync(writer, descriptor.Client);
                await writer.WriteLineAsync();

                await WriteAddEnumSerializersAsync(writer, descriptor);
                await writer.WriteLineAsync();

                await WriteAddInputSerializersAsync(writer, descriptor);
                await writer.WriteLineAsync();

                await WriteAddResultParsersAsync(writer, descriptor);
                await writer.WriteLineAsync();

                await WriteTryAddDefaultOperationSerializerAsync(writer);
                await writer.WriteLineAsync();

                await WriteTryAddDefaultHttpPipelineAsync(writer);
                await writer.WriteLineAsync();

                await WriteClientFactoryAsync(writer, descriptor.Client);
                await writer.WriteLineAsync();

                await WritePipelineFactoryAsync(writer, descriptor.Client);
            });

        private async Task WriteAddSerializersAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                "AddDefaultScalarSerializers",
                true,
                async () =>
                {
                    await writer.WriteIndentedLineAsync(
                        "foreach (IValueSerializer serializer in ValueSerializers.All)");
                    await writer.WriteIndentedLineAsync("{");

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.AddSingleton(serializer);");
                    }

                    await writer.WriteIndentedLineAsync("}");
                    await writer.WriteLineAsync();
                });
        }

        private async Task WriteAddClientAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                $"Add{descriptor.Name}",
                true,
                async () =>
                {
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddSingleton<{0}, {1}>();",
                        GetInterfaceName(descriptor.Name),
                        GetClassName(descriptor.Name));

                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddSingleton(sp =>");

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "HttpOperationExecutorBuilder.New()");
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                ".AddServices(sp)");
                            await writer.WriteIndentedLineAsync(
                                ".SetClient(ClientFactory)");
                            await writer.WriteIndentedLineAsync(
                                ".SetPipeline(PipelineFactory)");
                            await writer.WriteIndentedLineAsync(
                                ".Build());");
                        }
                    }
                    await writer.WriteLineAsync();

                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddDefaultScalarSerializers();");
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddEnumSerializers();");
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddInputSerializers();");
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.AddResultParsers();");

                    await writer.WriteLineAsync();

                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.TryAddDefaultOperationSerializer();");
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.TryAddDefaultHttpPipeline();");
                    await writer.WriteLineAsync();
                });
        }

        private async Task WriteAddEnumSerializersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                "AddEnumSerializers",
                false,
                async () =>
                {
                    foreach (IEnumDescriptor enumType in descriptor.EnumTypes)
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.AddSingleton<IValueSerializer, {0}>();",
                            GetClassName(enumType.Name + "ValueSerializer"));
                    }
                });
        }

        private async Task WriteAddInputSerializersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                "AddInputSerializers",
                false,
                async () =>
                {
                    foreach (IInputClassDescriptor inputType in descriptor.InputTypes)
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.AddSingleton<IValueSerializer, {0}>();",
                            GetClassName(inputType.Name + "Serializer"));
                    }
                });
        }

        private async Task WriteAddResultParsersAsync(
            CodeWriter writer,
            IServicesDescriptor descriptor)
        {
            await WriteMethodAsync(
                writer,
                "AddResultParsers",
                false,
                async () =>
                {
                    foreach (IResultParserDescriptor resultParser in descriptor.ResultParsers)
                    {
                        await writer.WriteIndentedLineAsync(
                            "serviceCollection.AddSingleton<IResultParser, {0}>();",
                            GetClassName(resultParser.Name));
                    }
                });
        }

        private async Task WriteTryAddDefaultOperationSerializerAsync(CodeWriter writer)
        {
            await WriteMethodAsync(
                writer,
                "TryAddDefaultOperationSerializer",
                false,
                async () =>
                {
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.TryAddSingleton<" +
                        "IOperationSerializer, JsonOperationSerializer>();");
                });
        }

        private async Task WriteTryAddDefaultHttpPipelineAsync(CodeWriter writer)
        {
            await WriteMethodAsync(
                writer,
                "TryAddDefaultHttpPipeline",
                false,
                async () =>
                {
                    await writer.WriteIndentedLineAsync(
                        "serviceCollection.TryAddSingleton<OperationDelegate>(");
                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "sp => HttpPipelineBuilder.New()");
                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentedLineAsync(
                                ".Use<CreateStandardRequestMiddleware>()");
                            await writer.WriteIndentedLineAsync(
                                ".Use<SendHttpRequestMiddleware>()");
                            await writer.WriteIndentedLineAsync(
                                ".Use<ParseSingleResultMiddleware>()");
                            await writer.WriteIndentedLineAsync(
                                ".Build(sp));");
                        }
                    }
                });
        }

        private async Task WriteClientFactoryAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await writer.WriteIndentedLineAsync(
                "private static Func<HttpClient> ClientFactory(IServiceProvider services)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "var clientFactory = services.GetRequiredService<IHttpClientFactory>();");
                await writer.WriteIndentedLineAsync(
                    "return () => clientFactory.CreateClient(\"{0}\");",
                    GetClassName(descriptor.Name));
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private async Task WritePipelineFactoryAsync(
            CodeWriter writer,
            IClientDescriptor descriptor)
        {
            await writer.WriteIndentedLineAsync(
                "private static OperationDelegate PipelineFactory(IServiceProvider services)");
            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "return services.GetRequiredService<OperationDelegate>();");
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private async Task WriteMethodAsync(
            CodeWriter writer,
            string methodName,
            bool isPublic,
            Func<Task> write)
        {
            await writer.WriteIndentedLineAsync(
                "{0} static IServiceCollection {1}(",
                isPublic ? "public" : "private",
                methodName);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "this IServiceCollection serviceCollection)");
            }

            await writer.WriteIndentedLineAsync("{");

            using (writer.IncreaseIndent())
            {
                if (isPublic)
                {
                    await writer.WriteIndentedLineAsync(
                        "if (serviceCollection is null)");

                    await writer.WriteIndentedLineAsync("{");

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentedLineAsync(
                            "throw new ArgumentNullException(nameof(serviceCollection));");
                    }

                    await writer.WriteIndentedLineAsync("}");
                    await writer.WriteLineAsync();
                }

                await write();

                await writer.WriteIndentedLineAsync(
                    "return serviceCollection;");
            }

            await writer.WriteIndentedLineAsync("}");
        }

        private static string CreateName(IClientDescriptor descriptor) =>
            descriptor.Name + "ServiceCollectionExtensions";
    }
}
