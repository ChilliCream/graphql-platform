using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// exception - String
    /// </summary>
    internal class ExecutionExceptionContainsMatch : IAssertion
    {
        private static readonly string _exceptionKey = "exception";

        private ExecutionExceptionContainsMatch(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(Dictionary<object, object> value)
        {
            return (value.ContainsKey(_exceptionKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ExecutionExceptionContainsMatch(value);
        }

        public Block CreateBlock(Statement header, Block whenBlock)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
