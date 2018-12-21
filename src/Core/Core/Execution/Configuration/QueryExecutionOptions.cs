using System;

namespace HotChocolate.Execution.Configuration
{
    public class QueryExecutionOptions
        : IQueryExecutionOptionsAccessor
    {
        private static TimeSpan _defaultAndMinExecutionTimeout =
            TimeSpan.FromMilliseconds(100);
        private TimeSpan _executionTimeout = _defaultAndMinExecutionTimeout;
        private int? _maxExecutionDepth;

        internal const int MinMaxExecutionDepth = 1;
        internal static readonly TimeSpan MinExecutionTimeout =
            _defaultAndMinExecutionTimeout;

        public TimeSpan ExecutionTimeout
        {
            get { return _executionTimeout; }
            set
            {
                _executionTimeout = (value < MinExecutionTimeout)
                    ? MinExecutionTimeout
                    : value;
            }
        }

        public int? MaxExecutionDepth
        {
            get { return _maxExecutionDepth; }
            set
            {
                if (value.HasValue && value >= MinMaxExecutionDepth)
                {
                    _maxExecutionDepth = value;
                }
            }
        }
    }
}
