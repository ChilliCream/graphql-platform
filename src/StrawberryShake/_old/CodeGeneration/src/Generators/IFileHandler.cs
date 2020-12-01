using System.Threading.Tasks;

namespace StrawberryShake.Generators
{
    public interface IFileHandler
    {
        void Register(ICodeDescriptor descriptor, ICodeGenerator generator);

        Task WriteAllAsync(ITypeLookup typeLookup);
    }
}
