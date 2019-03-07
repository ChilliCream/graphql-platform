using System.Collections.Generic;
using System.Linq;

namespace Generator
{
    internal class Scenario
    {
        public string Name { get; }

        public Scenario(string name, IEnumerable<Test> tests)
        {
            Name = string.Join("", name
                .Split(':', ' ')
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(l => l.UpperFirstLetter()));

            Tests = tests;
        }

        public object Background { get; set; }

        public IEnumerable<Test> Tests { get; }
    }
}
