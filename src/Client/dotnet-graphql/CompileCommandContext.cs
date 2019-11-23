namespace StrawberryShake.Tools
{
    public class CompileCommandContext
        : ICompileContext
    {
        public CompileCommandContext(
            string path, 
            bool search)
        {
            Path = path;
            Search = search;
        }

        public string Path { get; }
        public bool Search { get; }
    }
}
