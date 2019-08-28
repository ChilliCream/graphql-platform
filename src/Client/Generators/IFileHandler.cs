using System;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public interface IFileHandler
    {
        void WriteTo(string fileName, Func<CodeWriter, Task> write);

        Task WriteAllAsync();
    }
}
