using StrawberryShake.Generators;
using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Commands.Generate
{
    public class GenerateCommandContext : ICompileContext
    {
        public GenerateCommandContext(
            string path,
            LanguageVersion language,
            bool dISupport,
            string @namespace,
            string? persistedQueryFile,
            bool search,
            bool force)
        {
            Path = path;
            Language = language;
            DISupport = dISupport;
            Namespace = @namespace;
            PersistedQueryFile = persistedQueryFile;
            Search = search;
            Force = force;
        }

        public string Path { get; }
        public LanguageVersion Language { get; }
        public bool DISupport { get; }
        public string Namespace { get; }
        public string? PersistedQueryFile { get; }
        public bool Search { get; }
        public bool Force { get; }
    }
}
