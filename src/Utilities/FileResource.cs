using System.IO;

namespace HotChocolate
{
    public static class FileResource
    {
        public static string Open(string name)
        {
            string fielPath = Path.Combine(
                "__resources__", name);
            if (File.Exists(fielPath))
            {
                return File.ReadAllText(fielPath);
            }
            return null;
        }
    }
}
