namespace Mocha.Transport.Nats;

/// <summary>
/// Matches NATS subjects against filters containing the <c>*</c> and <c>&gt;</c> wildcards.
/// </summary>
public static class SubjectMatcher
{
    /// <summary>
    /// Removes subjects that another subject in the set already covers.
    /// </summary>
    /// <param name="subjects">The subjects to collapse.</param>
    /// <returns>The subjects with redundant entries removed, in the order they were first seen.</returns>
    /// <remarks>
    /// Only one subject covering another is removed. Two that overlap without either covering the
    /// other, such as <c>a.*.c</c> and <c>a.b.&gt;</c>, are both kept.
    /// </remarks>
    public static List<string> Collapse(IEnumerable<string> subjects)
    {
        ArgumentNullException.ThrowIfNull(subjects);

        var collapsed = new List<string>();

        foreach (var subject in subjects)
        {
            if (collapsed.Any(existing => Matches(existing, subject)))
            {
                continue;
            }

            collapsed.RemoveAll(existing => Matches(subject, existing));
            collapsed.Add(subject);
        }

        return collapsed;
    }

    /// <summary>
    /// Determines whether a subject contains a wildcard token, making it a filter rather than a
    /// subject a message can be published to.
    /// </summary>
    /// <param name="subject">The subject to test.</param>
    /// <returns><see langword="true"/> when the subject contains <c>*</c> or <c>&gt;</c>.</returns>
    public static bool IsWildcard(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        foreach (var token in subject.Split('.'))
        {
            if (token is "*" or ">")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a subject is captured by a filter.
    /// </summary>
    /// <param name="filter">The subject filter, which may contain wildcards.</param>
    /// <param name="subject">The concrete subject to test.</param>
    /// <returns><see langword="true"/> when the filter captures the subject.</returns>
    /// <remarks>
    /// <c>*</c> matches exactly one token and <c>&gt;</c> matches one or more trailing tokens.
    /// </remarks>
    public static bool Matches(string filter, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filter);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var filterTokens = filter.Split('.');
        var subjectTokens = subject.Split('.');

        for (var i = 0; i < filterTokens.Length; i++)
        {
            var filterToken = filterTokens[i];

            if (filterToken == ">")
            {
                return i < subjectTokens.Length;
            }

            if (i >= subjectTokens.Length)
            {
                return false;
            }

            if (filterToken != "*" && filterToken != subjectTokens[i])
            {
                return false;
            }
        }

        return filterTokens.Length == subjectTokens.Length;
    }
}
