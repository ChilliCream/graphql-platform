using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("generate")]
    public class Generate : Compile
    {
        public Generate(bool json, string path, string search, string? language, bool diSupport, string? ns, string? persistedQueryFile, bool search2, bool force) : base(json, path, search)
        {
            Path = path;
            Language = language;
            DISupport = diSupport;
            Namespace = ns;
            PersistedQueryFile = persistedQueryFile;
            Search = search2;
            Force = force;
        }

        [Option('p', "path", HelpText = "The directory where the client shall be located.")]
        public string? Path { get; }

        [Option('l', "languageVersion", HelpText = "The C# Language Version (7.3, 8.0 or 9.0).")]
        public string? Language { get; }

        [Option('d', "diSupport", HelpText = "Generate dependency injection for Microsoft.Extensions.DependencyInjection.GenerateCommandOptions")]
        public bool DISupport { get; }

        [Option('n', "namespace", HelpText = "The namespace that shall be used for the generated files.")]
        public string? Namespace { get; }

        [Option('q', "persistedQueryFile", HelpText = "The persisted query file.")]
        public string? PersistedQueryFile { get; }

        [Option('s', "search", HelpText = "Search for client directories.")]
        public bool Search { get; }

        [Option('f', "force", HelpText = "Force code generation even if nothing has changed.")]
        public bool Force { get; }

    }
}
