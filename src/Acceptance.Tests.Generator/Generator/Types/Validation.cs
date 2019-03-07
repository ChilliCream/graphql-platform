using System;
using System.IO;
using System.Linq;
using System.Text;
using Generator.ClassGenerator;
using HotChocolate;
using Path = System.IO.Path;

namespace Generator
{
    internal class Validation
    {
        public static Scenario Load(string sourceDir)
        {
            var fileContent = File.ReadAllText(
                Path.Combine(sourceDir, "validation", "ExecutableDefinitions.yaml"));

            return Deserializer
                .Deserialize(fileContent);
        }

        public static void Generate(Scenario definition, string outputDir)
        {
            ClassBuilder classBuilder = ClassBuilder.Init(definition.Name)
                .WithUsings("System", "System.IO", "HotChocolate", "HotChocolate.Language", "HotChocolate.Execution", "Xunit")
                .WithNamespace("Generated.Tests")
                .WithFields(
                    new Statement("private IQueryParser _parser;"),
                    new Statement("private Schema _schema;"))
                .WithConstructor(
                    new Statement("_parser = new DefaultQueryParser();"),
                    new Statement("var schemaContent = File.ReadAllText(\"validation.schema.graphql\");"),
                    new Statement("_schema = Schema.Create(schemaContent, c => c.Use(next => context => throw new NotImplementedException()));"))
                .WithMethods(definition.Tests.Select(t =>
                    new ClassMethod("void", t.Name, t.CreateBlock())).ToArray());

            Compilation compilation = classBuilder.Compile();
            StringBuilder builder = new StringBuilder();

            compilation.Errors
                .ForEach(error => builder.AppendLine($"// {error}"));

            builder.AppendLine(compilation.Source);

            var errorsFilePath = Path.Combine(outputDir, $"{definition.Name}_Tests.cs");
            File.WriteAllText(errorsFilePath, builder.ToString());
        }
    }
}
