using System;
using System.Collections.Generic;

namespace Generator
{
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
                Given given = GivenResolver.Resolve(test["given"]);
                IAction when = WhenResolver.Resolve(test["when"]);
                IEnumerable<IAssertion> then = ThenResolver.Resolve(test["then"]);

                yield return new Test(name, given, when, then);
            }
        }
    }
}
