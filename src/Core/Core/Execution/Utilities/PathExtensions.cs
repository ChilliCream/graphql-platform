namespace HotChocolate.Execution
{
    internal static class PathExtensions
    {
        public static Path AppendOrCreate(this Path path, NameString name)
        {
            return path == null
                ? Path.New(name)
                : path.Append(name);
        }
    }
}
