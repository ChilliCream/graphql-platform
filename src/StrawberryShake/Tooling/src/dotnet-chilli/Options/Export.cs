using CommandLine;
using HotChocolate;

namespace StrawberryShake.Tools.Options
{
    [Verb("export", HelpText = "Exports a GraphQL Schema from a .NET project by discovering a " + nameof(SchemaBuilderMethodAttribute))]
    public class Export : BaseOptions
    {
        public Export(bool json, string assembly, string? type, string? method, string? path) : base(json)
        {
            Assembly = assembly;
            Type = type;
            Method = method;
            Path = path;
        }

        [Option('a', "assembly", HelpText = ".NET Assembly to load")]
        public string Assembly { get; }

        [Option('t', "typeName", HelpText = "Name of type to retrieve SchemaBuilder method from")]
        public string? Type { get; }

        [Option('m', "method", HelpText = "SchemaBuilder method")]
        public string? Method { get; }

        public const string DefaultPath = "schema.graphql";

        [Option('p', "path", HelpText = "Output file", Default = DefaultPath)]
        public string? Path { get; }

    }
}
