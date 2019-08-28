using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public interface ICodeGenerator
    {
        bool CanHandle(ICodeDescriptor descriptor);

        Task WriteAsync(
            CodeWriter writer,
            ICodeDescriptor descriptor,
            ITypeLookup typeLookup);

        string CreateFileName(ICodeDescriptor descriptor);
    }
}
