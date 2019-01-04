using System;
using System.Diagnostics;

namespace HotChocolate.Execution.Configuration
{
    public class QueryExecutionOptions
        : IQueryExecutionOptionsAccessor
    {
        private const int _minMaxExecutionDepth = 1;
        private const int _minQueryCacheSize = 10;
        private static readonly TimeSpan _minExecutionTimeout =
            TimeSpan.FromMilliseconds(100);

        private TimeSpan _executionTimeout = TimeSpan.FromSeconds(30);
        private int? _maxExecutionDepth;
        private int _queryCacheSize = _minQueryCacheSize;
        private int? _maxOperationComplexity;

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

        public bool IncludeExceptionDetails { get; set; } = Debugger.IsAttached;

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
