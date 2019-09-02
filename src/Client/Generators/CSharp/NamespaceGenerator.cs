using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class NamespaceGenerator
        : ICodeGenerator
    {
        private ICodeGenerator _innerGenerator;

        public NamespaceGenerator(ICodeGenerator innerGenerator)
        {
            _innerGenerator = innerGenerator
                ?? throw new ArgumentNullException(nameof(innerGenerator));
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
            await WriteUsings(writer, _innerGenerator);
            await writer.WriteLineAsync();

            await writer.WriteAsync("namespace Foo");
            await writer.WriteLineAsync();
            await writer.WriteAsync('{');
            await writer.WriteLineAsync();

            using (writer.IncreaseIndent())
            {
                await _innerGenerator.WriteAsync(
                    writer, descriptor, typeLookup);
            }

            await writer.WriteAsync('}');
            await writer.WriteLineAsync();
        }

        private async Task WriteUsings(CodeWriter writer, ICodeGenerator generator)
        {
            var components = generator is IUsesComponents c
                ? new HashSet<string>(c.Components)
                : new HashSet<string>();

            await WriteUsing(writer, "System");
            await WriteUsing(writer, "System.Collections");
            await WriteUsing(writer, "System.Collections.Generic");

            if (components.Contains(WellKnownComponents.Json))
            {
                await WriteUsing(writer, "System.Text.Json");
            }

            if (components.Contains(WellKnownComponents.Task))
            {
                await WriteUsing(writer, "System.Threading");
                await WriteUsing(writer, "System.Threading.Tasks");
            }

            await WriteUsing(writer, "StrawberryShake");

            if (components.Contains(WellKnownComponents.Http))
            {
                await WriteUsing(writer, "StrawberryShake.Http");
            }
        }

        private async Task WriteUsing(CodeWriter writer, string ns)
        {
            await writer.WriteAsync($"using {ns};");
            await writer.WriteLineAsync();
        }
    }
}
