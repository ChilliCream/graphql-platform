using System;
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
        private readonly int _errorCounts;

        private ErrorCount(Dictionary<object, object> value)
        {
            _errorCounts = int.Parse(value[_errorCountKey] as string);
        }

        public static (bool, CreateAssertion) TryCreate(
            Dictionary<object, object> value,
            TestContext context)
        {
            return (value.ContainsKey(_errorCountKey), Create);
        }

        public static IAssertion Create(Dictionary<object, object> value)
        {
            return new ErrorCount(value);
        }

        public Block CreateBlock()
        {
            return new Block(new Statement($"Assert.Equal({_errorCounts}, result.Errors.Count);"));
        }
    }
}
