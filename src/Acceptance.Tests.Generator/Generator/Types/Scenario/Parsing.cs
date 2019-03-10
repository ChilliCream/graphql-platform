using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal class Parsing : IScenario
    {
        private Parsing(ScenarioDefinition definition)
        {
            Name = definition.Name;
            Background = definition.Background;
            Tests = definition.Tests;
        }

        public static IScenario Create(ScenarioDefinition definition)
        {
            return new Parsing(definition);
        }

        public string Name { get; }
        public Background Background { get; }
        public IEnumerable<Test> Tests { get; }

        public IEnumerable<string> Usings
        {
            get
            {
                yield return "HotChocolate.Language";
                yield return "HotChocolate.Execution";
                yield return "Xunit";
            }
        }

        public string Namespace { get; } = "Generated.Tests";

        public IEnumerable<Statement> Fields
        {
            get
            {
                yield return new Statement("private readonly IQueryParser _parser;");
            }
        }

        public IEnumerable<Statement> Constructor
        {
            get
            {
                yield return new Statement("_parser = new DefaultQueryParser();");
            }
        }
    }
}
