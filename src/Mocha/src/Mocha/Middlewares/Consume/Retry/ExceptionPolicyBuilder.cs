namespace Mocha;

internal sealed class ExceptionPolicyBuilder<TException>
    : IExceptionPolicyBuilder<TException>, IAfterRetryBuilder, IAfterRedeliveryBuilder
    where TException : Exception
{
    private readonly ExceptionPolicyRule _rule;
    private readonly List<ExceptionPolicyRule> _rules;
    private bool _committed;

    internal ExceptionPolicyBuilder(ExceptionPolicyRule rule, List<ExceptionPolicyRule> rules)
    {
        _rule = rule;
        _rules = rules;
    }

    private void EnsureCommitted()
    {
        if (!_committed)
        {
            // Replace any existing rule for the same exception type and predicate.
            for (var i = _rules.Count - 1; i >= 0; i--)
            {
                if (_rules[i].ExceptionType == _rule.ExceptionType
                    && _rules[i].Predicate == _rule.Predicate)
                {
                    _rules.RemoveAt(i);
                }
            }

            _rules.Add(_rule);
            _committed = true;
        }
    }

    // IExceptionPolicyBuilder<TException>

    public void Discard()
    {
        EnsureCommitted();
        _rule.Terminal = TerminalAction.Discard;
    }

    public void DeadLetter()
    {
        EnsureCommitted();
        _rule.Terminal = TerminalAction.DeadLetter;
    }

    public IAfterRetryBuilder Retry()
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig
        {
            Attempts = RetryPolicyDefaults.Attempts,
            Delay = RetryPolicyDefaults.Delay,
            Backoff = RetryPolicyDefaults.Backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };
        _rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };
        return this;
    }

    public IAfterRetryBuilder Retry(int attempts)
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig
        {
            Attempts = attempts,
            Delay = RetryPolicyDefaults.Delay,
            Backoff = RetryPolicyDefaults.Backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };
        _rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };
        return this;
    }

    public IAfterRetryBuilder Retry(
        int attempts,
        TimeSpan delay,
        RetryBackoffType backoff = RetryBackoffType.Exponential)
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig
        {
            Attempts = attempts,
            Delay = delay,
            Backoff = backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };
        _rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };
        return this;
    }

    public IAfterRetryBuilder Retry(TimeSpan[] intervals)
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig
        {
            Intervals = intervals,
            Attempts = intervals.Length
        };
        _rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };
        return this;
    }

    public IAfterRedeliveryBuilder Redeliver()
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig { Enabled = false };
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Intervals = RedeliveryPolicyDefaults.Intervals,
            Attempts = RedeliveryPolicyDefaults.Intervals.Length,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public IAfterRedeliveryBuilder Redeliver(int attempts, TimeSpan baseDelay)
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig { Enabled = false };
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Attempts = attempts,
            BaseDelay = baseDelay,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public IAfterRedeliveryBuilder Redeliver(TimeSpan[] intervals)
    {
        EnsureCommitted();
        _rule.Retry = new RetryPolicyConfig { Enabled = false };
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Intervals = intervals,
            Attempts = intervals.Length,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    // IAfterRetryBuilder

    public IAfterRedeliveryBuilder ThenRedeliver()
    {
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Intervals = RedeliveryPolicyDefaults.Intervals,
            Attempts = RedeliveryPolicyDefaults.Intervals.Length,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public IAfterRedeliveryBuilder ThenRedeliver(int attempts, TimeSpan baseDelay)
    {
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Attempts = attempts,
            BaseDelay = baseDelay,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public IAfterRedeliveryBuilder ThenRedeliver(TimeSpan[] intervals)
    {
        _rule.Redelivery = new RedeliveryPolicyConfig
        {
            Intervals = intervals,
            Attempts = intervals.Length,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public void ThenDeadLetter()
    {
        _rule.Terminal = TerminalAction.DeadLetter;
    }

    // IAfterRedeliveryBuilder.ThenDeadLetter() is the same method
}
