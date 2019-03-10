using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal class Validation : IScenario
    {
        private Validation(ScenarioDefinition definition)
        {
            Name = definition.Name;
            Background = definition.Background;
            Tests = definition.Tests;
        }

        public static IScenario Create(ScenarioDefinition definition)
        {
            return new Validation(definition);
        }

        public string Name { get; }
        public Background Background { get; }
        public IEnumerable<Test> Tests { get; }

        public IEnumerable<string> Usings
        {
            get
            {
                yield return "System";
                yield return "System.IO";
                yield return "System.Linq";
                yield return "HotChocolate";
                yield return "HotChocolate.Execution";
                yield return "HotChocolate.Validation";
                yield return "Microsoft.Extensions.DependencyInjection";
                yield return "Xunit";
            }
        }

        public string Namespace { get; } = "Generated.Tests";

        public IEnumerable<Statement> Fields
        {
            get
            {
                yield return new Statement("private readonly IQueryParser _parser;");
                yield return new Statement("private readonly Schema _schema;");
                yield return new Statement("private readonly ServiceProvider _serviceProvider;");
            }
        }

        public IEnumerable<Statement> Constructor
        {
            get
            {
                yield return new Statement("_parser = new DefaultQueryParser();");
                yield return new Statement("var schemaContent = File.ReadAllText(\"validation.schema.graphql\");");
                yield return new Statement("_schema = Schema.Create(schemaContent, c => c.Use(next => context => throw new NotImplementedException()));");
                yield return new Statement("var serviceCollection = new ServiceCollection();");
                yield return new Statement("serviceCollection.AddDefaultValidationRules();");
                yield return new Statement("serviceCollection.AddQueryValidation();");
                yield return new Statement("_serviceProvider = serviceCollection.BuildServiceProvider();");
            }
        }
    }
}
