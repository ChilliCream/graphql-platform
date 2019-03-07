using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error-regex - String
    /// </summary>
    internal class ExecutionExceptionRegexMatch : IAssertion
    {
        private static readonly string _errorRegexKey = "error-regex";

        private ExecutionExceptionRegexMatch(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKey(_errorRegexKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ExecutionExceptionRegexMatch(value);
        }

        public Block CreateBlock(Statement header)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
