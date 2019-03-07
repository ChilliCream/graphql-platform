using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// syntax-error - Boolean
    /// </summary>
    internal class ParsingSyntaxError : IAssertion
    {
        private static readonly string _syntaxErrorKey = "syntax-error";

        private ParsingSyntaxError(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKey(_syntaxErrorKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ParsingSyntaxError(value);
        }

        public Block CreateBlock(Statement header)
        {
            return new Block
            {
                header,
                new Statement("Assert.Throws<SyntaxException>(() =>"),
                new Statement("{"),
                Statement.WhenPlaceholder,
                new Statement("});")
            };
        }
    }
}
