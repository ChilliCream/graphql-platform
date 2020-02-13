using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration
{
    public interface ICodeGenerator
    {
        bool CanHandle(ICodeDescriptor descriptor);

        Task WriteAsync(CodeWriter writer, ICodeDescriptor descriptor);
    }

    public abstract class CodeGenerator<TDescriptor>
        : ICodeGenerator
        where TDescriptor : ICodeDescriptor
    {
        protected bool NullableRefTypes { get; } = false;

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
