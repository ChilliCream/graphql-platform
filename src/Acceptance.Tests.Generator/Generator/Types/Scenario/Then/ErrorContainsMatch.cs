using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error - String
    /// loc - Array of Objects | Array of Arrays of Numbers(optional)
    ///     line - Number
    ///     column - Number
    /// </summary>
    internal class ErrorContainsMatch : IAssertion
    {
        private static readonly string _errorKey = "error";
        private static readonly string _locKey = "loc";

        private ErrorContainsMatch(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKeys(_errorKey, _locKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorContainsMatch(value);
        }

        public Block CreateBlock(Statement header)
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
