using System.Collections.Generic;

namespace Generator
{
    internal class Scenario
    {
        public string Name { get; }

        public Scenario(string name, IEnumerable<Test> tests)
        {
            Name = name
                .Replace(" ", "_")
                .Replace(":", "");
            Tests = tests;
        }

        public object Background { get; set; }

        public IEnumerable<Test> Tests { get; }
    }
}
