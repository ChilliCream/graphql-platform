using System.IO;
using System.Linq;
using Generator.ClassGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator
{
    internal class Validation
    {
        public static ScenarioDefinition Load(string sourceDir)
        {
            var fileContent = File.ReadAllText(
                Path.Combine(sourceDir, "validation", "ExecutableDefinitions.yaml"));

            return Deserializer.Instance
                .Deserialize<ScenarioDefinition>(fileContent);
        }

        public static void Generate(ScenarioDefinition definition, string outputDir)
        {
            ClassBuilder builder = ClassBuilder.Init(definition.Scenario)
                .WithUsings("System", "HotChocolate", "HotChocolate.Language", "HotChocolate.Execution", "Xunit")
                .WithNamespace("Generated.Tests")
                .WithFields(new Statement("private IQueryParser _parser;"))
                .WithConstructor(new Statement("_parser = new DefaultQueryParser();"))
                .WithMethods(definition.Tests.Select(t =>
                    new ClassMethod("void", t.GetValidName(), t.CreateStatement())).ToArray());

            var testFilePath = Path.Combine(outputDir, $"{definition.Scenario}_Tests.cs");
            File.WriteAllText(testFilePath, SyntaxFactory.ParseCompilationUnit(builder.Build()).NormalizeWhitespace().ToFullString());
        }
    }
}
