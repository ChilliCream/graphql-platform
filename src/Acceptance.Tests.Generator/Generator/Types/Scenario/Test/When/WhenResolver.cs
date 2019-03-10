using System;
using System.Collections.Generic;

namespace Generator
{
    internal static class WhenResolver
    {
        /// <summary>
        /// Object - action that should be performed in the test. See the Actions section for a list of available actions.
        /// </summary>
        internal static IAction Resolve(object value, TestContext testContext)
        {
            var when = value as Dictionary<object, object>;
            if (when == null)
            {
                throw new InvalidOperationException("Invalid when structure");
            }

            return ActionFactory.Create(when, testContext);
        }
    }
}
