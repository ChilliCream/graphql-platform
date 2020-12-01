using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    public abstract class CodeGenerator<T>
        : ICodeGenerator
        where T : ICodeDescriptor
    {
        protected CodeGenerator(ClientGeneratorOptions options)
        {
            Options = options;
            ClientAccessModifier = options.ClientAccessModifier.ToString().ToLowerInvariant();
            ModelAccessModifier = options.ModelAccessModifier.ToString().ToLowerInvariant();
        }

        protected ClientGeneratorOptions Options { get; }

        public string ClientAccessModifier { get; set; }

        public string ModelAccessModifier { get; set; }

        public bool CanHandle(ICodeDescriptor descriptor)
        {
            return descriptor is T;
        }

        public string CreateFileName(ICodeDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (descriptor is T t)
            {
                return CreateFileName(t);
            }

            throw new ArgumentException(
                "The code generator expected " +
                $"descriptor type `{typeof(T).FullName}`.");
        }

        protected virtual string CreateFileName(T descriptor)
        {
            return GetClassName(descriptor.Name) + ".cs";
        }

        public Task WriteAsync(
            CodeWriter writer,
            ICodeDescriptor descriptor,
            ITypeLookup typeLookup)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (typeLookup is null)
            {
                throw new ArgumentNullException(nameof(typeLookup));
            }

            if (descriptor is T t)
            {
                return WriteAsync(writer, t, typeLookup);
            }

            throw new ArgumentException(
                "The code generator expected " +
                $"descriptor type `{typeof(T).FullName}`.");
        }

        protected abstract Task WriteAsync(
            CodeWriter writer,
            T descriptor,
            ITypeLookup typeLookup);

        protected Task WriteStaticClassAsync(
            CodeWriter writer,
            string typeName,
            Func<Task> write) =>
            WriteClassAsync(writer, typeName, true, null, write);

        protected Task WriteClassAsync(
            CodeWriter writer,
            string typeName,
            Func<Task> write) =>
            WriteClassAsync(writer, typeName, null, write);

        protected Task WriteClassAsync(
            CodeWriter writer,
            string typeName,
            IEnumerable<string>? implements,
            Func<Task> write) =>
            WriteClassAsync(writer, typeName, false, implements, write);

        private async Task WriteClassAsync(
            CodeWriter writer,
            string typeName,
            bool isStatic,
            IEnumerable<string>? implements,
            Func<Task> write)
        {
            if (isStatic)
            {
                await writer.WriteIndentedLineAsync(
                    $"{ModelAccessModifier} static partial class {typeName}")
                    .ConfigureAwait(false);
            }
            else
            {
                await writer.WriteIndentedLineAsync(
                    $"{ModelAccessModifier} partial class {typeName}")
                    .ConfigureAwait(false);
            }

            if (implements is { })
            {
                using (writer.IncreaseIndent())
                {
                    bool first = true;
                    foreach (string name in implements)
                    {
                        if (first)
                        {
                            first = false;
                            await writer.WriteIndentedLineAsync(": {0}", name)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteIndentedLineAsync(", {0}", name)
                                .ConfigureAwait(false);
                        }
                    }
                }
            }

            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                await write().ConfigureAwait(false);
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }
    }
}
