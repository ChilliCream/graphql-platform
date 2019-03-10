using System.IO;
using System.Linq;
using System.Text;
using Generator.ClassGenerator;
using Compilation = Generator.ClassGenerator.Compilation;

namespace Generator
{
    internal class Parsing
    {
        private static string _category = "parsing";

        public static Scenario Load(string sourceDir)
        {
            var fileContent = File.ReadAllText(
                Path.Combine(sourceDir, _category, "SchemaParser.yaml"));

            return Deserializer
                .Deserialize(fileContent);
        }

        public static void Generate(Scenario definition, string outputDir)
        {
            ClassBuilder classBuilder = ClassBuilder.Init(definition.Name)
                .WithUsings("HotChocolate.Language", "HotChocolate.Execution", "Xunit")
                .WithNamespace("Generated.Tests")
                .WithFields(new Statement("private readonly IQueryParser _parser;"))
                .WithConstructor(new Statement("_parser = new DefaultQueryParser();"))
                .WithMethods(definition.Tests.Select(t =>
                    new ClassMethod("void", t.Name, t.CreateBlock())).ToArray());

            Compilation compilation = classBuilder.Compile();
            StringBuilder builder = new StringBuilder();

            compilation.Errors
                .ForEach(error => builder.AppendLine($"// {error}"));

            builder.AppendLine(compilation.Source);

            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(outputDir, _category));
            var errorsFilePath = Path.Combine(directory.FullName, $"{definition.Name}Tests.cs");
            File.WriteAllText(errorsFilePath, builder.ToString());
        }
    }
}
