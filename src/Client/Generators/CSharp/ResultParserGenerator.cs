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
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public class ");
            await writer.WriteAsync(descriptor.Name);
            await writer.WriteLineAsync();

            await WriteImplementsAsync(writer, descriptor);

            await writer.WriteIndentAsync();
            await writer.WriteAsync("{");
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await WriteSerializerFieldsAsync(writer, descriptor);
                await writer.WriteLineAsync();

                await WriteConstructorAsync(writer, descriptor);
                await writer.WriteLineAsync();

                foreach (IResultParserMethodDescriptor method in
                    descriptor.ParseMethods)
                {
                    await _methodGenerator.WriteAsync(
                        writer, method, typeLookup);
                    await writer.WriteLineAsync();
                }

                await _desMethodGenerator.WriteAsync(
                    writer, descriptor, typeLookup);
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync("}");
            await writer.WriteLineAsync();
        }

        private static async Task WriteImplementsAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync(':');
                await writer.WriteSpaceAsync();
                await writer.WriteAsync("GeneratedResultParserBase<");
                await writer.WriteAsync(parserDescriptor.ResultDescriptor.Name);
                await writer.WriteAsync(">");
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteSerializerFieldsAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            foreach (INamedType leafType in parserDescriptor.InvolvedLeafTypes)
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("private readonly IValueSerializer");
                await writer.WriteSpaceAsync();
                await writer.WriteAsync('_');
                await writer.WriteAsync(GetFieldName(leafType.Name));
                await writer.WriteAsync("Serializer");
                await writer.WriteAsync(';');
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteConstructorAsync(
            CodeWriter writer,
            IResultParserDescriptor parserDescriptor)
        {
            await writer.WriteIndentAsync();
            await writer.WriteAsync("public ");
            await writer.WriteAsync(parserDescriptor.Name);
            await writer.WriteAsync("(IEnumerable<IValueSerializer> serializers)");
            await writer.WriteLineAsync();

            await writer.WriteIndentAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await writer.WriteIndentAsync();
                await writer.WriteAsync("IReadOnlyDictionary<string, IValueSerializer> map = ");
                await writer.WriteAsync("serializers.ToDictionary();");
                await writer.WriteLineAsync();

                for (int i = 0; i < parserDescriptor.InvolvedLeafTypes.Count; i++)
                {
                    INamedType leafType = parserDescriptor.InvolvedLeafTypes[i];

                    await writer.WriteLineAsync();
                    await writer.WriteIndentAsync();
                    await writer.WriteAsync(
                        "if (!map.TryGetValue" +
                        $"(\"{leafType.Name}\", out ");

                    if (i == 0)
                    {
                        await writer.WriteAsync("IValueSerializer");
                    }

                    await writer.WriteAsync(" serializer))");
                    await writer.WriteAsync('{');
                    await writer.WriteLineAsync();

                    using (writer.IncreaseIndent())
                    {
                        await writer.WriteIndentAsync();
                        await writer.WriteAsync("throw new ArgumentException(");
                        await writer.WriteLineAsync();

                        using (writer.IncreaseIndent())
                        {
                            await writer.WriteIndentAsync();
                            await writer.WriteAsync(
                                "\"There is no serializer specified for " +
                                $"`{leafType.Name}`.\",");
                            await writer.WriteLineAsync();

                            await writer.WriteIndentAsync();
                            await writer.WriteAsync("nameof(serializers));");
                            await writer.WriteLineAsync();
                        }
                    }

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('}');
                    await writer.WriteLineAsync();

                    await writer.WriteIndentAsync();
                    await writer.WriteAsync('_');
                    await writer.WriteAsync(GetFieldName(leafType.Name));
                    await writer.WriteAsync("Serializer = serializer;");
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteIndentAsync();
            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }
    }
}
