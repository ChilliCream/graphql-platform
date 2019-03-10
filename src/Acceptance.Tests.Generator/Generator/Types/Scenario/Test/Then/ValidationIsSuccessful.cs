using System;
using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// passes - Boolean
    /// </summary>
    internal class ValidationIsSuccessful : IAssertion
    {
        private static readonly string _passesKey = "passes";

        private ValidationIsSuccessful(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKey(_passesKey)
                    && context.Action == Actions.Validation, Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ValidationIsSuccessful(value);
        }

        public Block CreateBlock()
        {
            return new Block
            {
                new Statement("Assert.False(result.HasErrors);")
            };
        }
    }
}
