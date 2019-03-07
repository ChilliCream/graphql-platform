using System;
using System.Collections.Generic;

namespace Generator
{
    internal delegate IAssertion CreateAssertion(Dictionary<object, object> value);
    internal delegate (bool canCreate, CreateAssertion create) TryCreateAssertion(Dictionary<object, object> value, TestContext context);

    /// <summary>
    /// https://github.com/graphql-cats/graphql-cats#assertions
    /// </summary>
    internal static class AssertionFactory
    {
        private static readonly List<TryCreateAssertion> _assertions = new List<TryCreateAssertion>
        {
            ParsingIsSuccessful.TryCreate,
            ValidationIsSuccessful.TryCreate,
            ParsingSyntaxError.TryCreate,
            DataMatch.TryCreate,
            ErrorCount.TryCreate,
            ErrorCodeMatch.TryCreate,
            ErrorContainsMatch.TryCreate,
            ErrorRegexMatch.TryCreate,
            ExecutionExceptionContainsMatch.TryCreate,
            ExecutionExceptionRegexMatch.TryCreate
        };

        public static IAssertion Create(Dictionary<object, object> then, TestContext context)
        {
            foreach (TryCreateAssertion assertion in _assertions)
            {
                (bool canCreate, CreateAssertion create) assertionResult = assertion(then, context);
                if (assertionResult.canCreate)
                {
                    return assertionResult.create(then);
                }
            }

            throw new InvalidOperationException("Unknown assertion type");
        }
    }
}
