using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error-count - Number
    /// </summary>
    internal class ErrorCount : IAssertion
    {
        private static readonly string _errorCountKey = "error-count";

        private ErrorCount(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(Dictionary<object, object> value)
        {
            return (value.ContainsKey(_errorCountKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorCount(value);
        }

        public Block CreateBlock(Statement header, Block whenBlock)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
