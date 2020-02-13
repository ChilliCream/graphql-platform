using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class CodeFileBuilder
        : ICodeBuilder
    {
        private List<string> _usings = new List<string>();
        private string? _namespace;
        private List<ITypeBuilder> _types = new List<ITypeBuilder>();

        public static CodeFileBuilder New() => new CodeFileBuilder();

        public CodeFileBuilder AddUsing(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The namespace cannot be null or empty.",
                    nameof(value));
            }

            _usings.Add(value);
            return this;
        }

        public CodeFileBuilder SetNamespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The namespace cannot be null or empty.",
                    nameof(value));
            }

            _namespace = value;
            return this;
        }

        public CodeFileBuilder AddType(ITypeBuilder value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _types.Add(value);
            return this;
        }

        public Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_types.Count == 0 && _usings.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (_namespace is null)
            {
                throw new InvalidOperationException("Namespace has to be set.");
            }

            return BuildInternal(writer);
        }

        private async Task BuildInternal(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_types.Count == 0 && _usings.Count == 0)
            {
                return;
            }

            if (_namespace is null)
            {
                throw new InvalidOperationException("Namespace has to be set.");
            }

            if (_usings.Count > 0)
            {
                foreach (string u in _usings)
                {
                    await writer.WriteIndentedLineAsync($"using {u};").ConfigureAwait(false);
                }
                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync($"namespace {_namespace}").ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                foreach (ITypeBuilder type in _types)
                {
                    await type.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}
