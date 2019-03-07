using System.Collections.Generic;
using System.Linq;
using Generator.ClassGenerator;

namespace Generator
{
    internal class Test
    {
        private readonly Given _given;
        private readonly IAction _when;
        private readonly IEnumerable<IAssertion> _then;

        public Test(string name, Given given, IAction when, IEnumerable<IAssertion> then)
        {
            Name = name
                .Replace("-", "")
                .Replace(" ", "_");

            _given = given;
            _when = when;
            _then = then;
        }

        public string Name { get; }

        public Block CreateBlock()
        {
            // TODO: Create context with fields names
            var testBlock = new Block
            {
                _given.CreateBlock(new Statement("// Given"))
            };

            Block whenBlock = _when.CreateBlock(new Statement("// When"));
            foreach (IAssertion then in _then)
            {
                testBlock.Add(then.CreateBlock(new Statement("// Then"), whenBlock));
            }

            return testBlock;
        }
    }
}
