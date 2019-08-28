using System.IO;
using System;
using System.Threading.Tasks;

namespace StrawberryShake.Generators
{
    public interface IFileHandler
    {
        void WriteTo(string fileName, Func<Stream, Task> write);

        Task WriteAllAsync();
    }
}
