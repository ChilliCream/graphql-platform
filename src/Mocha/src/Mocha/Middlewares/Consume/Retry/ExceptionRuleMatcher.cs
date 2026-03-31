namespace Mocha;

/// <summary>
/// Evaluates exception rules to determine if an exception should be ignored.
/// Most-specific-type-wins: if both DbException.Ignore() and NpgsqlException rules exist,
/// NpgsqlException rule takes priority for NpgsqlException instances.
/// </summary>
internal static class ExceptionRuleMatcher
{
    /// <summary>
    /// Determines whether the given exception should be ignored based on the configured rules.
    /// </summary>
    /// <param name="rules">The list of exception rules to evaluate.</param>
    /// <param name="exception">The exception to match against.</param>
    /// <returns><c>true</c> if the exception should be ignored; otherwise, <c>false</c>.</returns>
    public static bool ShouldIgnore(IReadOnlyList<ExceptionRule> rules, Exception exception)
    {
        ExceptionRule? bestMatch = null;
        var bestDepth = int.MaxValue;

        foreach (var rule in rules)
        {
            if (!rule.ExceptionType.IsInstanceOfType(exception))
            {
                continue;
            }

            if (rule.Predicate is not null && !rule.Predicate(exception))
            {
                continue;
            }

            // Calculate inheritance depth: most specific type = smallest depth
            var depth = 0;
            var type = exception.GetType();

            while (type != null && type != rule.ExceptionType)
            {
                depth++;
                type = type.BaseType;
            }

            if (depth < bestDepth)
            {
                bestDepth = depth;
                bestMatch = rule;
            }
        }

        return bestMatch?.Action == ExceptionAction.Ignore;
    }
}
