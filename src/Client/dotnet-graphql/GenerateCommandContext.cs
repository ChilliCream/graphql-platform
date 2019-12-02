using StrawberryShake.Generators;

namespace StrawberryShake.Tools
{
    public class GenerateCommandContext
        : ICompileContext
    {
        public GenerateCommandContext(
            string path, 
            LanguageVersion language, 
            bool dISupport, 
            string @namespace, 
            bool search, 
            bool force)
        {
            Path = path;
            Language = language;
            DISupport = dISupport;
            Namespace = @namespace;
            Search = search;
            Force = force;
        }

        public string Path { get; }
        public LanguageVersion Language { get; }
        public bool DISupport { get; }
        public string Namespace { get; }
        public bool Search { get; }
        public bool Force { get; }
    }
}