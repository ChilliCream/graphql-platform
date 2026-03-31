namespace Mocha;

/// <summary>
/// Fluent builder for configuring per-exception retry/redelivery behavior.
/// </summary>
/// <typeparam name="TException">The exception type to configure behavior for.</typeparam>
public sealed class ExceptionPolicyBuilder<TException> where TException : Exception
{
    private readonly List<ExceptionRule> _rules;
    private readonly Func<TException, bool>? _predicate;

    internal ExceptionPolicyBuilder(List<ExceptionRule> rules, Func<TException, bool>? predicate)
    {
        _rules = rules;
        _predicate = predicate;
    }

    /// <summary>
    /// Excludes this exception type from retry/redelivery. The exception propagates
    /// immediately without being retried.
    /// </summary>
    public void Ignore()
    {
        Func<Exception, bool>? wrappedPredicate = _predicate is not null
            ? ex => ex is TException typed && _predicate(typed)
            : null;

        _rules.Add(new ExceptionRule
        {
            ExceptionType = typeof(TException),
            Predicate = wrappedPredicate,
            Action = ExceptionAction.Ignore
        });
    }
}
