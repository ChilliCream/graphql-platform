using System.Collections.Generic;

namespace Generator
{
    internal class ScenarioDefinition
    {
        public string Scenario { get; set; }
        public IList<Test> Tests { get; set; }
    }
}