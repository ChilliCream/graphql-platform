using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly ResultParserDeserializeMethodGenerator _desMethodGenerator =
            new ResultParserDeserializeMethodGenerator();
        private static readonly ResultParserMethodGenerator _methodGenerator =
            new ResultParserMethodGenerator();

        public IReadOnlyList<string> Components { get; } = new[]
        {
            WellKnownComponents.Json,
            WellKnownComponents.Http,
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
                "(IEnumerable<IValueSerializer> serializers)")
                .ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('{').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync().ConfigureAwait(false);
                await writer.WriteAsync(
                    "IReadOnlyDictionary<string, IValueSerializer> map = ")
                    .ConfigureAwait(false);
                await writer.WriteAsync("serializers.ToDictionary();").ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);

                for (int i = 0; i < parserDescriptor.InvolvedLeafTypes.Count; i++)
                {
                    INamedType leafType = parserDescriptor.InvolvedLeafTypes[i];

                    await writer.WriteLineAsync().ConfigureAwait(false);
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(
                        "if (!map.TryGetValue" +
                        $"(\"{leafType.Name}\", out ")
                        .ConfigureAwait(false);

                    if (i == 0)
                    {
                        await writer.WriteAsync("IValueSerializer").ConfigureAwait(false);
                    }

                    await writer.WriteAsync(" serializer))").ConfigureAwait(false);
                    await writer.WriteAsync('{').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await writer.WriteAsync(
                            "throw new ArgumentException(")
                            .ConfigureAwait(false);
                        await writer.WriteLineAsync().ConfigureAwait(false);

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentAsync().ConfigureAwait(false);
                            await writer.WriteAsync(
                                "\"There is no serializer specified for " +
                                $"`{leafType.Name}`.\",")
                                .ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);

                            await writer.WriteIndentAsync().ConfigureAwait(false);
                            await writer.WriteAsync("nameof(serializers));").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                    }

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('}').ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);

                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync('_').ConfigureAwait(false);
                    await writer.WriteAsync(GetFieldName(leafType.Name)).ConfigureAwait(false);
                    await writer.WriteAsync("Serializer = serializer;").ConfigureAwait(false);
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteIndentAsync().ConfigureAwait(false);
            await writer.WriteAsync('}').ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
