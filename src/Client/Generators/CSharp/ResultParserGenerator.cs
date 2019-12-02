using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators.CSharp
{
    public class ResultParserGenerator
        : CodeGenerator<IResultParserDescriptor>
        , IUsesComponents
    {
        private static readonly ResultParserMethodGenerator _methodGenerator =
            new ResultParserMethodGenerator();
        private readonly ResultParserDeserializeMethodGenerator _desMethodGenerator;
        private readonly ClientGeneratorOptions _options;

        public ResultParserGenerator(ClientGeneratorOptions options)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _desMethodGenerator = new ResultParserDeserializeMethodGenerator(
                options.LanguageVersion);
        }

        public IReadOnlyList<string> Components { get; } = new[]
        {
            WellKnownComponents.Json,
            WellKnownComponents.HttpExecutor,
        };

        protected override async Task WriteAsync(
            CodeWriter writer,
            IResultParserDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public class ").ConfigureAwait(false);
            await writer.WriteAsync(descriptor.Name).ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await WriteImplementsAsync(writer, descriptor).ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("{").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await WriteSerializerFieldsAsync(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                await WriteConstructorAsync(writer, descriptor).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                foreach (IResultParserMethodDescriptor method in
                    descriptor.ParseMethods)
                {
                    await _methodGenerator.WriteAsync(
                        writer, method, typeLookup)
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                await _desMethodGenerator.WriteAsync(
                    writer, descriptor, typeLookup)
                    .ConfigureAwait(false);
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        private static async Task WriteImplementsAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(':').ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync("JsonResultParserBase<").ConfigureAwait(false);
                await writer.WriteAsync(
                    parserDescriptor.ResultDescriptor.Name)
                    .ConfigureAwait(false);
                await writer.WriteAsync(">").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteSerializerFieldsAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            foreach (INamedType leafType in parserDescriptor.InvolvedLeafTypes)
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync("private readonly IValueSerializer").ConfigureAwait(false);
                await writer.WriteSpaceAsync().ConfigureAwait(false);
                await writer.WriteAsync('_').ConfigureAwait(false);
                await writer.WriteAsync(GetFieldName(leafType.Name)).ConfigureAwait(false);
                await writer.WriteAsync("Serializer").ConfigureAwait(false);
                await writer.WriteAsync(';').ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteConstructorAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync("public ").ConfigureAwait(false);
            await writer.WriteAsync(parserDescriptor.Name).ConfigureAwait(false);
            await writer.WriteAsync(
                "(IValueSerializerResolver serializerResolver)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentedLineAsync(
                    "if (serializerResolver is null)")
                    .ConfigureAwait(false);
                await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentedLineAsync(
                        "throw new ArgumentNullException(nameof(serializerResolver));")
                        .ConfigureAwait(false);
                }
                await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);

                for (int i = 0; i < parserDescriptor.InvolvedLeafTypes.Count; i++)
                {
                    INamedType leafType = parserDescriptor.InvolvedLeafTypes[i];

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('_').ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(leafType.Name)).ConfigureAwait(false);
                    await writer.WriteAsync(
                        "Serializer = serializerResolver.GetValueSerializer(" +
                        $"\"{leafType.Name}\");")
                        .ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
