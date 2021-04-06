using System;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public abstract class CodeGenerator<TDescriptor>
        : ICodeGenerator
        where TDescriptor : ICodeDescriptor
    {
        public bool CanHandle(
            ICodeDescriptor descriptor,
            CodeGeneratorSettings settings) =>
            descriptor is TDescriptor d && CanHandle(d, settings);

        protected virtual bool CanHandle(TDescriptor descriptor, CodeGeneratorSettings settings) => true;

        public void Generate(ICodeDescriptor descriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Generate((TDescriptor)descriptor, settings, writer, out fileName, out path);
        }

        protected abstract void Generate(
            TDescriptor descriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path);

        protected static string State => nameof(State);
        protected static string DependencyInjection => nameof(DependencyInjection);
        protected static string Serialization => nameof(Serialization);
    }
}
