namespace Mocha;

/// <summary>
/// Options for configuring the retry middleware that retries failed message handler invocations
/// with configurable backoff strategies.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Gets or sets whether retry is enabled. Null inherits from parent scope; defaults to true.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum retry attempts (not counting the initial attempt).
    /// </summary>
    public int? MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the base delay between retries. Interpretation depends on <see cref="BackoffType"/>.
    /// </summary>
    public TimeSpan? Delay { get; set; }

    /// <summary>
    /// Gets or sets the maximum delay cap. Prevents exponential backoff from growing unbounded.
    /// </summary>
    public TimeSpan? MaxDelay { get; set; }

    /// <summary>
    /// Gets or sets the backoff strategy: Constant, Linear, or Exponential.
    /// </summary>
    public RetryBackoffType? BackoffType { get; set; }

    /// <summary>
    /// Gets or sets whether to add jitter to delay calculations.
    /// </summary>
    public bool? UseJitter { get; set; }

    /// <summary>
    /// Gets or sets explicit retry intervals. When set, overrides <see cref="Delay"/>,
    /// <see cref="BackoffType"/>, and <see cref="MaxRetryAttempts"/>.
    /// The number of elements determines the number of retries.
    /// </summary>
    public TimeSpan[]? Intervals { get; set; }

    private List<ExceptionRule>? _exceptionRules;

    internal IReadOnlyList<ExceptionRule> ExceptionRules => _exceptionRules ?? (IReadOnlyList<ExceptionRule>)[];

    /// <summary>
    /// Configures behavior for a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type to configure.</typeparam>
    /// <returns>A builder for configuring the exception behavior.</returns>
    public ExceptionPolicyBuilder<TException> On<TException>() where TException : Exception
        => On<TException>(null);

    /// <summary>
    /// Configures behavior for a specific exception type matching a predicate.
    /// </summary>
    /// <typeparam name="TException">The exception type to configure.</typeparam>
    /// <param name="predicate">An optional predicate to further filter the exception.</param>
    /// <returns>A builder for configuring the exception behavior.</returns>
    public ExceptionPolicyBuilder<TException> On<TException>(Func<TException, bool>? predicate)
        where TException : Exception
    {
        _exceptionRules ??= [];
        var builder = new ExceptionPolicyBuilder<TException>(_exceptionRules, predicate);
        return builder;
    }

    /// <summary>
    /// Provides the default values for retry options.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// The default maximum retry attempts.
        /// </summary>
        public static int MaxRetryAttempts = 3;

        /// <summary>
        /// The default base delay between retries.
        /// </summary>
        public static TimeSpan Delay = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// The default maximum delay cap.
        /// </summary>
        public static TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The default backoff strategy.
        /// </summary>
        public static RetryBackoffType BackoffType = RetryBackoffType.Exponential;

        /// <summary>
        /// The default jitter setting.
        /// </summary>
        public static bool UseJitter = true;
    }
}
