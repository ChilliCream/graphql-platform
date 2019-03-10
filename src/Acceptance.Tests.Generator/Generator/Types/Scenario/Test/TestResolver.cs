using System;
using System.Collections.Generic;

namespace Generator
{
    internal class TestContext
    {
        public Actions Action { get; set; }
    }

    internal static class TestResolver
    {
        internal static IEnumerable<Test> Resolve(object value)
        {
            var tests = value as List<object>;
            if (tests == null)
            {
                throw new InvalidOperationException("Scenario must have a list of tests");
            }

            for (int i = 0; i < tests.Count; i++)
            {
                var test = tests[i] as Dictionary<object, object>;
                if (test == null)
                {
                    throw new InvalidOperationException("Invalid test structure");
                }

                var name = test["name"] as string;
                var testContext = new TestContext();
                Given given = GivenResolver.Resolve(test["given"], testContext);
                IAction when = WhenResolver.Resolve(test["when"], testContext);
                IEnumerable<IAssertion> then = ThenResolver.Resolve(test["then"], testContext);

                yield return new Test(name, given, when, then);
            }
        }
    }
}
