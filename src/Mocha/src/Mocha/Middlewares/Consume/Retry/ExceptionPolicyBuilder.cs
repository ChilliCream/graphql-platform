namespace Mocha;

internal sealed class ExceptionPolicyBuilder<TException>(ExceptionPolicyRule rule, List<ExceptionPolicyRule> rules)
    : IExceptionPolicyBuilder<TException>
    , IAfterRetryBuilder
    , IAfterRedeliveryBuilder where TException : Exception
{
    private bool _committed;

    public void Discard()
    {
        EnsureCommitted();

        rule.Terminal = TerminalAction.Discard;
    }

    public void DeadLetter()
    {
        EnsureCommitted();

        rule.Terminal = TerminalAction.DeadLetter;
    }

    public IAfterRetryBuilder Retry()
    {
        EnsureCommitted();

        rule.Retry = new RetryPolicyConfig
        {
            Attempts = RetryPolicyDefaults.Attempts,
            Delay = RetryPolicyDefaults.Delay,
            Backoff = RetryPolicyDefaults.Backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };
        rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };
        return this;
    }

    public IAfterRetryBuilder Retry(int attempts)
    {
        EnsureCommitted();

        rule.Retry = new RetryPolicyConfig
        {
            Attempts = attempts,
            Delay = RetryPolicyDefaults.Delay,
            Backoff = RetryPolicyDefaults.Backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };
        rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };

        return this;
    }

    public IAfterRetryBuilder Retry(
        int attempts,
        TimeSpan delay,
        RetryBackoffType backoff = RetryBackoffType.Exponential)
    {
        EnsureCommitted();

        rule.Retry = new RetryPolicyConfig
        {
            Attempts = attempts,
            Delay = delay,
            Backoff = backoff,
            UseJitter = RetryPolicyDefaults.UseJitter,
            MaxDelay = RetryPolicyDefaults.MaxDelay
        };

        rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };

        return this;
    }

    public IAfterRetryBuilder Retry(TimeSpan[] intervals)
    {
        EnsureCommitted();

        rule.Retry = new RetryPolicyConfig { Intervals = intervals, Attempts = intervals.Length };
        rule.Redelivery = new RedeliveryPolicyConfig { Enabled = false };

        return this;
    }

    public IAfterRedeliveryBuilder Redeliver()
    {
        EnsureCommitted();

        rule.Retry = new RetryPolicyConfig { Enabled = false };
        rule.Redelivery = new RedeliveryPolicyConfig
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

        rule.Retry = new RetryPolicyConfig { Enabled = false };
        rule.Redelivery = new RedeliveryPolicyConfig
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

        rule.Retry = new RetryPolicyConfig { Enabled = false };
        rule.Redelivery = new RedeliveryPolicyConfig
        {
            Intervals = intervals,
            Attempts = intervals.Length,
            UseJitter = RedeliveryPolicyDefaults.UseJitter,
            MaxDelay = RedeliveryPolicyDefaults.MaxDelay
        };
        return this;
    }

    public IAfterRedeliveryBuilder ThenRedeliver()
    {
        rule.Redelivery = new RedeliveryPolicyConfig
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
        rule.Redelivery = new RedeliveryPolicyConfig
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
        rule.Redelivery = new RedeliveryPolicyConfig
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
        rule.Terminal = TerminalAction.DeadLetter;
    }

    private void EnsureCommitted()
    {
        if (!_committed)
        {
            // Replace any existing rule for the same exception type and predicate.
            for (var i = rules.Count - 1; i >= 0; i--)
            {
                if (rules[i].ExceptionType == rule.ExceptionType
                    && rules[i].Predicate == rule.Predicate)
                {
                    rules.RemoveAt(i);
                }
            }

            rules.Add(rule);
            _committed = true;
        }
    }
}
