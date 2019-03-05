using System.Collections.Generic;
namespace Generator
{
    internal class ScenarioDefinition
    {
        private string _scenario;

        public string Scenario
        {
            get => _scenario.Replace(" ", "_");
            set => _scenario = value;
        }

        public IList<Test> Tests { get; set; }

        private string Identifier => $"{Scenario}_Tests";
    }
}
