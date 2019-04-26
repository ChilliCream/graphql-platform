using System.IO;

namespace HotChocolate.Types
{
    public class Upload
    {
        public string FileName { get; set; }
        public Stream Stream { get; set; }

        public Upload(string fileName, Stream stream)
        {
            FileName = fileName;
            Stream = stream;
        }
    }
}
