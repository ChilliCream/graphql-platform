using System.IO;
using System.Linq;
using System.Text;
using Generator.ClassGenerator;
using Compilation = Generator.ClassGenerator.Compilation;

namespace Generator
{
    internal class Parsing
    {
        public static Scenario Load(string sourceDir)
        {
            var fileContent = File.ReadAllText(
                Path.Combine(sourceDir, "parsing", "SchemaParser.yaml"));

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
            if (compilation.Errors.Any())
            {
                StringBuilder builder = new StringBuilder();

                compilation.Errors
                    .ForEach(error => builder.AppendLine(error));

                builder.AppendLine(compilation.Source);

                var errorsFilePath = Path.Combine(outputDir, $"{definition.Name}_Errors.txt");
                File.WriteAllText(errorsFilePath, builder.ToString());
            }
            else
            {
                var testFilePath = Path.Combine(outputDir, $"{definition.Name}_Tests.cs");
                File.WriteAllText(testFilePath, classBuilder.Build());
            }
        }
    }
}
