using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration
{
    public interface ICodeGenerator
    {
        bool CanHandle(ICodeDescriptor descriptor);

        Task WriteAsync(CodeWriter writer, ICodeDescriptor descriptor);
    }
}
