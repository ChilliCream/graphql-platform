using System;
using System.Diagnostics;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents the entirety of settings to configure the behavior of the
    /// query execution engine.
    /// </summary>
    public class QueryExecutionOptions
        : IQueryExecutionOptionsAccessor
    {
        private const int _minMaxExecutionDepth = 1;
        private const int _minQueryCacheSize = 10;
        private static readonly TimeSpan _minExecutionTimeout =
            TimeSpan.FromMilliseconds(100);

        private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);
        private int? _maxExecutionDepth;
        private int _queryCacheSize = 100;
        private int? _maxOperationComplexity;

        /// <summary>
        /// Gets or sets maximum allowed execution time of a query. The default
        /// value is <c>30</c> seconds. The minimum allowed value is <c>100</c>
        /// milliseconds.
        /// </summary>
        public TimeSpan ExecutionTimeout
        {
            get => _executionTimeout;
            set
            {
                _executionTimeout = (value < _minExecutionTimeout)
                    ? _minExecutionTimeout
                    : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <c>GraphQL</c> errors
        /// should be extended with exception details. The default value is
        /// <see cref="Debugger.IsAttached"/>.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } =
            Debugger.IsAttached;

        /// <summary>
        /// Gets or sets the maximum allowed depth of a query. The default
        /// value is <see langword="null"/>. The minimum allowed value is
        /// <c>1</c>.
        /// </summary>
        public int? MaxExecutionDepth
        {
            get => _maxExecutionDepth;
            set
            {
                if (value.HasValue && value < _minMaxExecutionDepth)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "MaxExecutionDepth mustn't be below one.");
                }

                _maxExecutionDepth = value;
            }
        }

        public int? MaxOperationComplexity
        {
            get => _maxOperationComplexity;
            set
            {
                if (value.HasValue && value < _maxOperationComplexity)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "MaxOperationComplexity mustn't be below one.");
                }

                _maxOperationComplexity = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of queries that can be cached. The
        /// default value is <c>100</c>. The minimum allowed value is
        /// <c>10</c>.
        /// </summary>
        public int QueryCacheSize
        {
            get => _queryCacheSize;
            set
            {
                _queryCacheSize = (value < _minQueryCacheSize)
                    ? _minQueryCacheSize
                    : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing for performance
        /// measurement of query requests is enabled. The default value is
        /// <see cref="TracingPreference.Never"/>.
        /// </summary>
        public TracingPreference TracingPreference { get; set; }

        public bool? UseComplexityMultipliers { get; set; }

        /// <summary>
        /// Defines that the query graph shall be traversed and execution serially.
        /// </summary>
        public bool? ForceSerialExecution { get; set; }
    }
}
