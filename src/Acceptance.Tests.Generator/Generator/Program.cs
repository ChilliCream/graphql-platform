using System;
using System.IO;

namespace Generator
{
    class Program
    {
        private static readonly string _scenariosRootDir = "_scenarios";

        static void Main(string[] args)
        {
            var outputPath = ResovleOutputPath();

            ScenarioDefinition scenarios = Parsing.Load(_scenariosRootDir);
            Parsing.Generate(scenarios, outputPath);
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
