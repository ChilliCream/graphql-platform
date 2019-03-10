using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Generator.ClassGenerator;
using Path = System.IO.Path;

namespace Generator
{
    internal class Validation
    {
        private static readonly List<string> _files = new List<string>
        {
            "ExecutableDefinitions.yaml",
            "FieldsOnCorrectType.yaml",
            "FragmentsOnCompositeTypes.yaml",
            "KnownArgumentNames.yaml",
            "ScalarLeafs.yaml",
        };

        public static IEnumerable<Scenario> Load(string sourceDir)
        {
            foreach (var file in _files)
            {
                var fileContent = File.ReadAllText(
                    Path.Combine(sourceDir, "validation", file));

                yield return Deserializer
                    .Deserialize(fileContent);
            }
        }

        public static void Generate(IEnumerable<Scenario> definitions, string outputDir)
        {
            foreach (Scenario definition in definitions)
            {
                ClassBuilder classBuilder = ClassBuilder.Init(definition.Name)
                .WithUsings(
                    "System",
                    "System.IO",
                    "System.Linq",
                    "HotChocolate",
                    "HotChocolate.Execution",
                    "HotChocolate.Validation",
                    "Microsoft.Extensions.DependencyInjection",
                    "Xunit")
                .WithNamespace("Generated.Tests")
                .WithFields(
                    new Statement("private readonly IQueryParser _parser;"),
                    new Statement("private readonly Schema _schema;"),
                    new Statement("private readonly ServiceProvider _serviceProvider;"))
                .WithConstructor(
                    new Statement("_parser = new DefaultQueryParser();"),
                    new Statement("var schemaContent = File.ReadAllText(\"validation.schema.graphql\");"),
                    new Statement("_schema = Schema.Create(schemaContent, c => c.Use(next => context => throw new NotImplementedException()));"),
                    new Statement("var serviceCollection = new ServiceCollection();"),
                    new Statement("serviceCollection.AddDefaultValidationRules();"),
                    new Statement("serviceCollection.AddQueryValidation();"),
                    new Statement("_serviceProvider = serviceCollection.BuildServiceProvider();"))
                .WithMethods(definition.Tests.Select(t =>
                    new ClassMethod("void", t.Name, t.CreateBlock())).ToArray());

                Compilation compilation = classBuilder.Compile();
                StringBuilder builder = new StringBuilder();

                compilation.Errors
                    .ForEach(error => builder.AppendLine($"// {error}"));

                builder.AppendLine(compilation.Source);

                var errorsFilePath = Path.Combine(outputDir, $"{definition.Name}Tests.cs");
                File.WriteAllText(errorsFilePath, builder.ToString());
            }
        }
    }
}
