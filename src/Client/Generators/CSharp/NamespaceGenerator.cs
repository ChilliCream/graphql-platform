using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class NamespaceGenerator
        : ICodeGenerator
    {
        private readonly ICodeGenerator _innerGenerator;
        private readonly string _namespace;

        public NamespaceGenerator(ICodeGenerator innerGenerator, string ns)
        {
            _innerGenerator = innerGenerator
                ?? throw new ArgumentNullException(nameof(innerGenerator));
            _namespace = ns?.Trim() ?? throw new ArgumentNullException(nameof(ns));
        }

        public bool CanHandle(ICodeDescriptor descriptor) =>
            _innerGenerator.CanHandle(descriptor);

        public string CreateFileName(ICodeDescriptor descriptor) =>
            _innerGenerator.CreateFileName(descriptor);

        public async Task WriteAsync(
            CodeWriter writer,
            ICodeDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await WriteUsings(writer, _innerGenerator).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteAsync($"namespace {_namespace}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await _innerGenerator.WriteAsync(
                    writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
            }

            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteUsings(CodeWriter writer, ICodeGenerator generator)
        {
            var components = generator is IUsesComponents c
                ? new HashSet<string>(c.Components)
                : new HashSet<string>();

            await WriteUsing(writer, "System").ConfigureAwait(false);
            await WriteUsing(writer, "System.Collections").ConfigureAwait(false);
            await WriteUsing(writer, "System.Collections.Generic").ConfigureAwait(false);

            if (components.Contains(WellKnownComponents.Http))
            {
                await WriteUsing(writer, "System.Net.Http").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.Json))
            {
                await WriteUsing(writer, "System.Text.Json").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.Task))
            {
                await WriteUsing(writer, "System.Threading").ConfigureAwait(false);
                await WriteUsing(writer, "System.Threading.Tasks").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.DI))
            {
                await WriteUsing(writer, "Microsoft.Extensions.DependencyInjection")
                    .ConfigureAwait(false);
                await WriteUsing(writer, "Microsoft.Extensions.DependencyInjection.Extensions")
                    .ConfigureAwait(false);
            }

            await WriteUsing(writer, "StrawberryShake").ConfigureAwait(false);

            if (components.Contains(WellKnownComponents.HttpExecutor))
            {
                await WriteUsing(writer, "StrawberryShake.Http").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.HttpExecutorPipeline))
            {
                await WriteUsing(writer, "StrawberryShake.Http.Pipelines").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.HttpExecutor))
            {
                await WriteUsing(writer, "StrawberryShake.Http.Subscriptions")
                    .ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.Serializer))
            {
                await WriteUsing(writer, "StrawberryShake.Serializers").ConfigureAwait(false);
            }

            if (components.Contains(WellKnownComponents.HttpExecutor))
            {
                await WriteUsing(writer, "StrawberryShake.Transport").ConfigureAwait(false);
            }
        }

        private static async Task WriteUsing(CodeWriter writer, string ns)
        {
            await writer.WriteAsync($"using {ns};").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
