using System.IO;

namespace HotChocolate
{
    public static class FileResource
    {
        public static string Open(string name)
        {
            string filePath = Path.Combine(
                "__resources__", name);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }
    }
}
