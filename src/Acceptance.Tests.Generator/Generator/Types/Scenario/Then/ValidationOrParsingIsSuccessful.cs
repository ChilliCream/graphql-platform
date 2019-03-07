using System;
using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// passes - Boolean
    /// </summary>
    internal class ValidationOrParsingIsSuccessful : IAssertion
    {
        private static readonly string _passesKey = "passes";

        private ValidationOrParsingIsSuccessful(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(Dictionary<object, object> value)
        {
            return (value.ContainsKey(_passesKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ValidationOrParsingIsSuccessful(value);
        }

        public Block CreateBlock(Statement header, Block whenBlock)
        {
            return new Block
            {
                whenBlock,
                header,
                new Statement("Assert.NotNull(document);")
            };
        }
    }
}
