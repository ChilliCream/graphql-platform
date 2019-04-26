using System.IO;

namespace HotChocolate.AspNetCore
{
    internal class ClientQueryRequestFile
    {
        public string Name { get; }
        public string FileName { get; }
        public Stream Stream { get; }

        public ClientQueryRequestFile(string name, string fileName, Stream stream)
        {
            Name = name;
            FileName = fileName;
            Stream = stream;
        }
    }
}
