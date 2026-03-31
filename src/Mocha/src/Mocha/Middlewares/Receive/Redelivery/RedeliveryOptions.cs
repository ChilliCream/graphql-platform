namespace Mocha;

/// <summary>
/// Options for configuring the redelivery middleware that reschedules failed messages
/// for later delivery with configurable delay strategies.
/// </summary>
public class RedeliveryOptions
{
    /// <summary>
    /// Gets or sets whether redelivery is enabled. Null inherits from parent scope; defaults to true.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of redelivery attempts.
    /// </summary>
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// Gets or sets the base delay for backoff calculation. Actual delay = BaseDelay * (attempt + 1).
    /// </summary>
    public TimeSpan? BaseDelay { get; set; }

    /// <summary>
    /// Gets or sets the maximum delay cap for any single redelivery delay.
    /// </summary>
    public TimeSpan? MaxDelay { get; set; }

    /// <summary>
    /// Gets or sets whether to add jitter to delay calculations.
    /// </summary>
    public bool? UseJitter { get; set; }

    /// <summary>
    /// Gets or sets explicit redelivery intervals. When set, overrides <see cref="BaseDelay"/>
    /// and <see cref="MaxAttempts"/>. The number of elements determines the number of
    /// redelivery attempts.
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
    /// Provides the default values for redelivery options.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// The default redelivery intervals: 5 minutes, 15 minutes, 30 minutes.
        /// </summary>
        public static TimeSpan[] Intervals =
        [
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30)
        ];

        /// <summary>
        /// The default maximum delay cap.
        /// </summary>
        public static TimeSpan MaxDelay = TimeSpan.FromHours(1);

        /// <summary>
        /// The default jitter setting.
        /// </summary>
        public static bool UseJitter = true;
    }
}
