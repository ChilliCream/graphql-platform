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

        public void Generate(CodeWriter writer, ICodeDescriptor descriptor, out string fileName)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Generate(writer, (TDescriptor)descriptor, out fileName);
        }


        protected abstract void Generate(
            CodeWriter writer,
            TDescriptor descriptor,
            out string fileName);
    }
}
