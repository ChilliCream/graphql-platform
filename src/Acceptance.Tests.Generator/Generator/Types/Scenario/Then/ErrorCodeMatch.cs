using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error-code - String
    /// args - Object(optional)
    /// loc - Array of Objects | Array of Arrays of Numbers(optional)
    ///     line - Number
    ///     column - Number
    /// </summary>
    internal class ErrorCodeMatch : IAssertion
    {
        private static readonly string _errorCodeKey = "error-code";
        private static readonly string _argsKey = "args";
        private static readonly string _locKey = "loc";

        private ErrorCodeMatch(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(Dictionary<object, object> value)
        {
            return (value.ContainsKeys(_errorCodeKey, _argsKey, _locKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorCodeMatch(value);
        }

        public Block CreateBlock(Statement header, Block whenBlock)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
