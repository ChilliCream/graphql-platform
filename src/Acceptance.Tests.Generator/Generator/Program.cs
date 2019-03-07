using System;
using System.IO;
using HotChocolate;
using Path = System.IO.Path;

namespace Generator
{
    class Program
    {
        private static readonly string _scenariosRootDir = "_scenarios";

        static void Main(string[] args)
        {
            var outputPath = ResovleOutputPath();

            Scenario parsingScenarios = Parsing.Load(_scenariosRootDir);
            Parsing.Generate(parsingScenarios, outputPath);

            Scenario validationScenario = Validation.Load(_scenariosRootDir);
            Validation.Generate(validationScenario, outputPath);

        }

        private static string ResovleOutputPath()
        {
            var runningPath = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo current = Directory.GetParent(runningPath);

            while (current != null && current.Name != "Acceptance.Tests.Generator")
            {
                current = current.Parent;
            }

            var testProjectPath = Path.Combine(current.FullName, "Tests");
            return testProjectPath;
        }
    }
}
