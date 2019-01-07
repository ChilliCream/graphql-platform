using System;
using System.Collections.Concurrent;
using System.Linq;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResultBuilder
        : IApolloTracingResultBuilder
    {
        private const int _apolloTracingVersion = 1;
        private const long _ticksToNanosecondsMultiplicator = 100;
        private readonly ConcurrentQueue<ApolloTracingResolverStatistics>
            _resolverResults =
                new ConcurrentQueue<ApolloTracingResolverStatistics>();
        private TimeSpan _duration;
        private ApolloTracingOperationResult _parsingResult;
        private DateTimeOffset _startTime;
        private long _startTimestamp;
        private ApolloTracingOperationResult _validationResult;

        public void SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp)
        {
            _startTime = startTime;
            _startTimestamp = startTimestamp;
        }

        public void SetParsingResult(long startTimestamp, long endTimestamp)
        {
            _parsingResult = new ApolloTracingOperationResult
            {
                StartOffset = startTimestamp - _startTimestamp,
                Duration = endTimestamp - startTimestamp
            };
        }

        public void SetValidationResult(long startTimestamp, long endTimestamp)
        {
            _validationResult = new ApolloTracingOperationResult
            {
                StartOffset = startTimestamp - _startTimestamp,
                Duration = endTimestamp - startTimestamp
            };
        }

        public void AddResolverResult(
            ApolloTracingResolverStatistics resolverStatistics)
        {
            _resolverResults.Enqueue(resolverStatistics);
        }

        public void SetRequestDuration(TimeSpan duration)
        {
            _duration = duration;
        }

        public ApolloTracingResult Build()
        {
            ApolloTracingExecutionResult executionResult = null;

            if (_resolverResults.Count > 0)
            {
                executionResult = new ApolloTracingExecutionResult
                {
                    Resolvers = _resolverResults
                        .Select(r => new ApolloTracingResolverResult
                        {
                            Path = r.Path,
                            ParentType = r.ParentType,
                            FieldName = r.FieldName,
                            ReturnType = r.ReturnType,
                            StartOffset = r.StartTimestamp - _startTimestamp,
                            Duration = r.EndTimestamp - r.StartTimestamp
                        })
                        .ToArray()
                };
            }

            return new ApolloTracingResult
            {
                Version = _apolloTracingVersion,
                StartTime = _startTime.ToRfc3339DateTimeString(),
                EndTime = _startTime.Add(_duration).ToRfc3339DateTimeString(),
                Duration = _duration.Ticks * _ticksToNanosecondsMultiplicator,
                Parsing = _parsingResult,
                Validation = _validationResult,
                Execution = executionResult
            };
        }
    }
}
