using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    /// <summary>
    /// error-regex - String
    /// loc - Array of Objects | Array of Arrays of Numbers(optional)
    ///     line - Number
    ///     column - Number
    /// </summary>
    internal class ErrorRegexMatch : IAssertion
    {
        private static readonly string _errorRegexKey = "error-regex";
        private static readonly string _locKey = "loc";

        private ErrorRegexMatch(Dictionary<object, object> value)
        {
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKeys(_errorRegexKey, _locKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorRegexMatch(value);
        }

        public Block CreateBlock()
        {
            return new Block(new Statement("throw new NotImplementedException();"));
        }
    }
}
