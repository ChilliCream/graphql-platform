using System;
using System.Collections.Generic;
using System.IO;
using HotChocolate;
using Path = System.IO.Path;

namespace Generator
{
    class Program
    {
        private static readonly string _scenariosRootDir = "_scenarios";

        private static readonly Dictionary<string, Func<ScenarioDefinition, IScenario>> _scenarios =
            new Dictionary<string, Func<ScenarioDefinition, IScenario>>
            {
                { Path.Combine(_scenariosRootDir, "parsing"), Parsing.Create },
                { Path.Combine(_scenariosRootDir, "validation"), Validation.Create }
            };

        static void Main(string[] args)
        {
            var outputPath = FindOutputPath();

            foreach (var entry in Directory.EnumerateDirectories(_scenariosRootDir))
            {
                if (_scenarios.ContainsKey(entry))
                {
                    foreach (var file in Directory.EnumerateFiles(entry, "*.yaml"))
                    {
                        var fileContent = File.ReadAllText(file);
                        ScenarioDefinition definition = Deserializer
                            .Deserialize(fileContent);

                        IScenario scenario = _scenarios[entry](definition);
                        ScenarioGeneration.Generate(scenario, Path.Combine(outputPath, entry));
                    }
                }
            }
        }

        private static string FindOutputPath()
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
