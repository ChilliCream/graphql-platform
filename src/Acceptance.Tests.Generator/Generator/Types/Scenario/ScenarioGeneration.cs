using System.IO;
using System.Linq;
using System.Text;
using Generator.ClassGenerator;

namespace Generator
{
    internal static class ScenarioGeneration
    {
        internal static void Generate(IScenario scenario, string outputDirectory)
        {
            scenario.CreateBackground();

            ClassBuilder classBuilder = ClassBuilder.Init(scenario.Name)
                .WithUsings(scenario.Usings)
                .WithNamespace(scenario.Namespace)
                .WithFields(scenario.Fields)
                .WithConstructor(scenario.Constructor)
                .WithMethods(scenario.Tests.Select(t =>
                    new ClassMethod("void", t.Name, t.CreateBlock())).ToArray());

            Compilation compilation = classBuilder.Compile();
            StringBuilder builder = new StringBuilder();

            compilation.Errors
                .ForEach(error => builder.AppendLine($"// {error}"));

            builder.AppendLine(compilation.Source);

            DirectoryInfo directory = Directory.CreateDirectory(outputDirectory);
            var errorsFilePath = Path.Combine(directory.FullName, $"{scenario.Name}Tests.cs");
            File.WriteAllText(errorsFilePath, builder.ToString());
        }
    }
}
