using System;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public abstract class CodeGenerator<TDescriptor>
        : ICodeGenerator
        where TDescriptor : ICodeDescriptor
    {
        public bool CanHandle(
            CodeGeneratorSettings settings,
            ICodeDescriptor descriptor) =>
            descriptor is TDescriptor d && CanHandle(settings, d);

        protected virtual bool CanHandle(
            CodeGeneratorSettings settings,
            TDescriptor descriptor) => true;

        public void Generate(ICodeDescriptor descriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            ICodeDescriptor descriptor,
            CodeGeneratorSettings settings,
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

            Generate(writer, (TDescriptor)descriptor, settings, out fileName, out path);
        }

        protected abstract void Generate(
            TDescriptor descriptor,
            CodeGeneratorSettings settings,
            out string fileName,
            out string? path);

        protected static string State => nameof(State);
        protected static string DependencyInjection => nameof(DependencyInjection);
        protected static string Serialization => nameof(Serialization);
    }
}
