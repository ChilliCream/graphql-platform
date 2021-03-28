using System;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public abstract class CodeGenerator<TDescriptor>
        : ICodeGenerator
        where TDescriptor : ICodeDescriptor
    {
        public bool CanHandle(ICodeDescriptor descriptor) =>
            descriptor is TDescriptor d && CanHandle(d);

        protected virtual bool CanHandle(TDescriptor descriptor) => true;

        public void Generate(
            CodeWriter writer,
            ICodeDescriptor descriptor,
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

            Generate(writer, (TDescriptor)descriptor, out fileName, out path);
        }

        protected abstract void Generate(
            CodeWriter writer,
            TDescriptor descriptor,
            out string fileName,
            out string? path);

        protected static string State => nameof(State);
        protected static string DependencyInjection => nameof(DependencyInjection);
        protected static string Serialization => nameof(Serialization);
    }
}
