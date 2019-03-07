using System;
using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// passes - Boolean
    /// </summary>
    internal class ParsingIsSuccessful : IAssertion
    {
        private static readonly string _passesKey = "passes";

        private ParsingIsSuccessful(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKey(_passesKey)
                    && context.Action == Actions.Parsing, Create);
        }

        public static IAssertion Create(
            Dictionary<object, object> value)
        {
            return new ParsingIsSuccessful(value);
        }

        public Block CreateBlock(Statement header)
        {
            return new Block
            {
                Statement.WhenPlaceholder,
                header,
                new Statement("Assert.NotNull(document);")
            };
        }
    }
}
