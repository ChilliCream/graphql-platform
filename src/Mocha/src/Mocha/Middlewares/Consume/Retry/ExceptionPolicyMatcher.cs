namespace Mocha;

/// <summary>
/// Evaluates exception policy rules to find the best matching rule for an exception.
/// Most-specific-type-wins: if both DbException and NpgsqlException rules exist,
/// NpgsqlException rule takes priority for NpgsqlException instances.
/// </summary>
internal static class ExceptionPolicyMatcher
{
    /// <summary>
    /// Finds the best matching exception policy rule for the given exception.
    /// </summary>
    /// <param name="rules">The list of exception policy rules to evaluate.</param>
    /// <param name="exception">The exception to match against.</param>
    /// <returns>The best matching rule, or <c>null</c> if no rule matches.</returns>
    public static ExceptionPolicyRule? Match(IReadOnlyList<ExceptionPolicyRule> rules, Exception exception)
    {
        ExceptionPolicyRule? bestMatch = null;
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

        return bestMatch;
    }
}
