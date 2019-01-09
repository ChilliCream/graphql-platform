using System;
using System.Diagnostics;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents the entirety of settings to configure the behaviour of the
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

        /// <summary>
        /// Gets or sets a value indicating whether tracing for performance
        /// measurement of query requests is enabled. The default value is
        /// <see langword="false"/>.
        /// </summary>
        public bool EnableTracing { get; set; }

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
                _maxExecutionDepth =
                    (value.HasValue && value < _minMaxExecutionDepth)
                        ? _minMaxExecutionDepth
                        : value;
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
    }
}
