using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration
{
    public abstract class CodeGenerator<TDescriptor>
        : ICodeGenerator
        where TDescriptor : ICodeDescriptor
    {
        public bool CanHandle(ICodeDescriptor descriptor) =>
            descriptor is TDescriptor d && CanHandle(d);

        protected virtual bool CanHandle(TDescriptor descriptor) => true;

        public Task WriteAsync(
            CodeWriter writer,
            ICodeDescriptor descriptor) =>
            WriteAsync(writer, (TDescriptor)descriptor);

        protected abstract Task WriteAsync(
            CodeWriter writer,
            TDescriptor descriptor);
    }
}
