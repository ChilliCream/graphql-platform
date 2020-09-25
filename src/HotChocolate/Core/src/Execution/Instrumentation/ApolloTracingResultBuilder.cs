using System;
using System.Collections.Concurrent;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Instrumentation.ApolloTracingResultKeys;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResultBuilder
    {
        private const int _apolloTracingVersion = 1;
        private const long _ticksToNanosecondsMultiplicator = 100;
        private readonly ConcurrentQueue<ApolloTracingResolverRecord> _resolverRecords =
            new ConcurrentQueue<ApolloTracingResolverRecord>();
        private TimeSpan _duration;
        private ResultMap? _parsingResult;
        private DateTimeOffset _startTime;
        private long _startTimestamp;
        private ResultMap? _validationResult;

        public void SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp)
        {
            _startTime = startTime;
            _startTimestamp = startTimestamp;
        }

        public void SetParsingResult(long startTimestamp, long endTimestamp)
        {
            _parsingResult = new ResultMap();
            _parsingResult.EnsureCapacity(2);
            _parsingResult.SetValue(0, StartOffset, startTimestamp - _startTimestamp);
            _parsingResult.SetValue(1, Duration, endTimestamp - startTimestamp);
        }

        public void SetValidationResult(long startTimestamp, long endTimestamp)
        {
            _validationResult = new ResultMap();
            _validationResult.EnsureCapacity(2);
            _validationResult.SetValue(0, StartOffset, startTimestamp - _startTimestamp);
            _validationResult.SetValue(1, Duration, endTimestamp - startTimestamp);
        }

        public void AddResolverResult(ApolloTracingResolverRecord record)
        {
            _resolverRecords.Enqueue(record);
        }

        public void SetRequestDuration(TimeSpan duration)
        {
            _duration = duration;
        }

        public IResultMap Build()
        {
            if (_parsingResult is null)
            {
                // in the case that the request pipeline cached the parsed document,
                // we will set the parsing duration to 0.
                SetParsingResult(_startTimestamp, _startTimestamp);
            }

            if (_validationResult is null)
            {
                // in the case that the request pipeline cached the validation result,
                // we will set the validation duration to 0.
                SetValidationResult(_startTimestamp, _startTimestamp);
            }

            var executionResult = new ResultMap();
            executionResult.EnsureCapacity(1);
            executionResult.SetValue(0, ApolloTracingResultKeys.Resolvers, BuildResolverResults());

            var result = new ResultMap();
            result.EnsureCapacity(7);
            result.SetValue(0, ApolloTracingResultKeys.Version, _apolloTracingVersion);
            result.SetValue(1, StartTime, _startTime.ToRfc3339DateTimeString());
            result.SetValue(2, EndTime, _startTime.Add(_duration).ToRfc3339DateTimeString());
            result.SetValue(3, Duration, _duration.Ticks * _ticksToNanosecondsMultiplicator);
            result.SetValue(4, Parsing, _parsingResult);
            result.SetValue(5, ApolloTracingResultKeys.Validation, _validationResult);
            result.SetValue(6, ApolloTracingResultKeys.Execution, executionResult);
            return result;
        }

        private ResultMap[] BuildResolverResults()
        {
            var i = 0;
            var results = new ResultMap[_resolverRecords.Count];

            foreach (ApolloTracingResolverRecord record in _resolverRecords)
            {
                var result = new ResultMap();
                result.EnsureCapacity(6);
                result.SetValue(0, ApolloTracingResultKeys.Path, record.Path);
                result.SetValue(1, ParentType, record.ParentType);
                result.SetValue(2, FieldName, record.FieldName);
                result.SetValue(3, ReturnType, record.ReturnType);
                result.SetValue(4, StartOffset, record.StartTimestamp - _startTimestamp);
                result.SetValue(5, Duration, record.EndTimestamp - record.StartTimestamp);
                results[i++] = result;
            }

            return results;
        }
    }
}
