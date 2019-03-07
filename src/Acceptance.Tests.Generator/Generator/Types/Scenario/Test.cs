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
                new Statement("// Given"),
                _given.CreateBlock()
            };

            Block whenBlock = new Block
            {
                new Statement("// When"),
                _when.CreateBlock()
            };
            Block thenBlock = new Block
            {
                new Statement("// Then")
            };
            foreach (IAssertion then in _then)
            {
                thenBlock.Add(then.CreateBlock());
            }

            if (!thenBlock.Replace(Statement.WhenPlaceholder, whenBlock))
            {
                testBlock.Add(whenBlock);
            }

            testBlock.Add(thenBlock);

            return testBlock;
        }
    }
}
