using System.Threading.Tasks;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public interface ICodeGenerator<T>
        where T : ICodeDescriptor
    {
        Task WriteAsync(
            CodeWriter writer,
            T descriptor,
            ITypeLookup typeLookup);
    }
}
