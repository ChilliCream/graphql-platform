using System;
using System.Collections.Generic;
using System.Linq;

namespace Generator
{
    internal static class ThenResolver
    {
        /// <summary>
        /// Object | Arrays of Objects - assertions that verify result of an action. See the Assertions section for a list of 
        /// </summary>
        internal static IEnumerable<IAssertion> Resolve(object value, TestContext testContext)
        {
            if (value is Dictionary<object, object> then)
            {
                return new[] { AssertionFactory.Create(then, testContext) };
            }

            if (value is List<object> thens && thens.TrueForAll(t => t is Dictionary<object, object>))
            {
                return thens
                    .Select(t => AssertionFactory.Create(t as Dictionary<object, object>, testContext));
            }

            throw new InvalidOperationException("Unknown assertion type");
        }
    }
}
