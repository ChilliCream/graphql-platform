namespace Mocha;

/// <summary>
/// Options for configuring exception policies with per-exception rules.
/// </summary>
public class ExceptionPolicyOptions
{
    private readonly List<ExceptionPolicyRule> _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionPolicyOptions"/> class.
    /// </summary>
    /// <param name="rules">The shared list of exception policy rules to populate.</param>
    public ExceptionPolicyOptions(List<ExceptionPolicyRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Configures the default behavior for all exceptions that don't match a more specific rule.
    /// Equivalent to <c>On&lt;Exception&gt;()</c>.
    /// </summary>
    /// <returns>A builder for configuring the default exception behavior.</returns>
    public IExceptionPolicyBuilder<Exception> Default() => On<Exception>(null);

    /// <summary>
    /// Configures behavior for a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type to configure.</typeparam>
    /// <returns>A builder for configuring the exception behavior.</returns>
    public IExceptionPolicyBuilder<TException> On<TException>() where TException : Exception
        => On<TException>(null);

    /// <summary>
    /// Configures behavior for a specific exception type matching a predicate.
    /// </summary>
    /// <typeparam name="TException">The exception type to configure.</typeparam>
    /// <param name="predicate">An optional predicate to further filter the exception.</param>
    /// <returns>A builder for configuring the exception behavior.</returns>
    public IExceptionPolicyBuilder<TException> On<TException>(Func<TException, bool>? predicate)
        where TException : Exception
    {
        Func<Exception, bool>? wrappedPredicate = predicate is not null
            ? ex => ex is TException typed && predicate(typed)
            : null;
        var rule = new ExceptionPolicyRule
        {
            ExceptionType = typeof(TException),
            Predicate = wrappedPredicate
        };
        return new ExceptionPolicyBuilder<TException>(rule, _rules);
    }
}
