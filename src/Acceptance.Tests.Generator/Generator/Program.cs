using System;
using System.IO;
using System.Linq;
using Generator.ClassGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var runningPath = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo current = Directory.GetParent(runningPath);

            while (current != null && current.Name != "Acceptance.Tests.Generator")
            {
                current = current.Parent;
            }

            var testProjectPath = Path.Combine(current.FullName, "Tests");

            var fileContent = File.ReadAllText(
                Path.Combine("_scenarios","parsing", "SchemaParser.yaml"));
            
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithNamingConvention(new HyphenatedNamingConvention())
                .Build();
            ScenarioDefinition definition = deserializer
                .Deserialize<ScenarioDefinition>(fileContent);

            ClassBuilder builder = ClassBuilder.Init(definition.Scenario)
                .WithUsings("System", "HotChocolate", "HotChocolate.Language", "HotChocolate.Execution", "Xunit")
                .WithNamespace("Generated.Tests")
                .WithFields(new Statement("private IQueryParser _parser;"))
                .WithConstructor(new Statement("_parser = new DefaultQueryParser();"))
                .WithMethods(definition.Tests.Select(t =>
                    new ClassMethod("void", t.GetValidName(), t.CreateStatement())).ToArray());

            var testFilePath = Path.Combine(testProjectPath, $"{definition.Scenario}_Tests.cs");
            File.WriteAllText(testFilePath, SyntaxFactory.ParseCompilationUnit(builder.Build()).NormalizeWhitespace().ToFullString());
        }
    }
}
