using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Tracing
{
    internal sealed class ApolloTracingResultBuilder
        : IApolloTracingResultBuilder
    {
        private const int _apolloTracingVersion = 1;
        private const string _dateTimeFormat =
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK";
        private const long _ticksToNanosecondsMultiplicator = 100;
        private readonly HashSet<ApolloTracingResolverResult> _resolverResults =
            new HashSet<ApolloTracingResolverResult>();
        private TimeSpan _duration;
        private ApolloTracingParsingResult _parsingResult;
        private DateTimeOffset _startTime;
        private long _startTimestamp;
        private ApolloTracingValidationResult _validationResult;

        public IApolloTracingResultBuilder SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp)
        {
            _startTime = startTime;
            _startTimestamp = startTimestamp;

            return this;
        }

        public IApolloTracingResultBuilder SetParsingResult(
            long startOffset,
            long duration)
        {
            _parsingResult = new ApolloTracingParsingResult
            {
                StartOffset = startOffset,
                Duration = duration
            };

            return this;
        }

        public IApolloTracingResultBuilder SetValidationResult(
            long startOffset,
            long duration)
        {
            _validationResult = new ApolloTracingValidationResult
            {
                StartOffset = startOffset,
                Duration = duration
            };

            return this;
        }

        public IApolloTracingResultBuilder AddResolverResult(
            ApolloTracingResolverResult result)
        {
            if (!_resolverResults.Contains(result))
            {
                _resolverResults.Add(result);
            }

            return this;
        }

        public IApolloTracingResultBuilder SetRequestDuration(
            TimeSpan duration)
        {
            _duration = duration;

            return this;
        }

        public ApolloTracingResult Build()
        {
            ApolloTracingExecutionResult executionResult = null;

            if (_resolverResults.Count > 0)
            {
                executionResult = new ApolloTracingExecutionResult
                {
                    Resolvers = _resolverResults
                };
            }

            return new ApolloTracingResult
            {
                Version = _apolloTracingVersion,
                StartTime = _startTime.ToString(_dateTimeFormat),
                EndTime = _startTime.Add(_duration).ToString(_dateTimeFormat),
                Duration = _duration.Ticks * _ticksToNanosecondsMultiplicator,
                Parsing = _parsingResult,
                Validation = _validationResult,
                Execution = executionResult
            };
        }
    }
}
