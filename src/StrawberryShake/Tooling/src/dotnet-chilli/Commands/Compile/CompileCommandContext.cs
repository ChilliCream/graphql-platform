using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Commands.Compile
{
    public class CompileCommandContext : ICompileContext
    {
        public CompileCommandContext(string path, bool search)
        {
            Path = path;
            Search = search;
        }

        public string Path { get; }
        public bool Search { get; }
    }
}
