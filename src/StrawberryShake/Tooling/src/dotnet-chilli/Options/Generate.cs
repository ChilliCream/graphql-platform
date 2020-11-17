using CommandLine;

namespace StrawberryShake.Tools.Options
{
    [Verb("generate")]
    public class Generate : Compile
    {
        [Option('p', "path", HelpText = "The directory where the client shall be located.")]
        public string? Path { get; set; }

        [Option('l', "languageVersion", HelpText = "The C# Language Version (7.3, 8.0 or 9.0).")]
        public string? Language { get; set; }

        [Option('d', "diSupport", HelpText = "Generate dependency injection for Microsoft.Extensions.DependencyInjection.GenerateCommandOptions")]
        public bool DISupport { get; set; }

        [Option('n', "namespace", HelpText = "The namespace that shall be used for the generated files.")]
        public string? Namespace { get; set; }

        [Option('q', "persistedQueryFile", HelpText = "The persisted query file.")]
        public string? PersistedQueryFile { get; set; }

        [Option('s', "search", HelpText = "Search for client directories.")]
        public bool Search { get; set; }

        [Option('f', "force", HelpText = "Force code generation even if nothing has changed.")]
        public bool Force { get; set; }

        [Option('j', "json", HelpText = "Console output as JSON.")]
        public bool Json { get; set; }

    }
}
