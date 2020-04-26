using System;
using System.Collections.Concurrent;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResultBuilder
    {
        private const int _apolloTracingVersion = 1;
        private const long _ticksToNanosecondsMultiplicator = 100;
        private readonly ConcurrentQueue<ApolloTracingResolverRecord>
            _resolverRecords =
                new ConcurrentQueue<ApolloTracingResolverRecord>();
        private TimeSpan _duration;
        private OrderedDictionary _parsingResult;
        private DateTimeOffset _startTime;
        private long _startTimestamp;
        private OrderedDictionary _validationResult;

        public void SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp)
        {
            _startTime = startTime;
            _startTimestamp = startTimestamp;
        }

        public void SetParsingResult(long startTimestamp, long endTimestamp)
        {
            _parsingResult = new OrderedDictionary
            {
                {
                    ApolloTracingResultKeys.StartOffset,
                    startTimestamp - _startTimestamp
                },
                {
                    ApolloTracingResultKeys.Duration,
                    endTimestamp - startTimestamp
                }
            };
        }

        public void SetValidationResult(long startTimestamp, long endTimestamp)
        {
            _validationResult = new OrderedDictionary
            {
                {
                    ApolloTracingResultKeys.StartOffset,
                    startTimestamp - _startTimestamp
                },
                {
                    ApolloTracingResultKeys.Duration,
                    endTimestamp - startTimestamp
                }
            };
        }

        public void AddResolverResult(
            ApolloTracingResolverRecord record)
        {
            _resolverRecords.Enqueue(record);
        }

        public void SetRequestDuration(TimeSpan duration)
        {
            _duration = duration;
        }

        public OrderedDictionary Build()
        {
            return new OrderedDictionary
            {
                {
                    ApolloTracingResultKeys.Version,
                    _apolloTracingVersion
                },
                {
                    ApolloTracingResultKeys.StartTime,
                    _startTime.ToRfc3339DateTimeString()
                },
                {
                    ApolloTracingResultKeys.EndTime,
                    _startTime.Add(_duration).ToRfc3339DateTimeString()
                },
                {
                    ApolloTracingResultKeys.Duration,
                    _duration.Ticks * _ticksToNanosecondsMultiplicator
                },
                {
                    ApolloTracingResultKeys.Parsing,
                    _parsingResult
                },
                {
                    ApolloTracingResultKeys.Validation,
                    _validationResult
                },
                {
                    ApolloTracingResultKeys.Execution,
                    new OrderedDictionary
                    {
                        {
                            ApolloTracingResultKeys.Resolvers,
                            BuildResolverResults()
                        }
                    }
                }
            };
        }

        private OrderedDictionary[] BuildResolverResults()
        {
            ApolloTracingResolverRecord[] records =
                _resolverRecords.ToArray();
            var resolvers = new OrderedDictionary[records.Length];

            for (var i = 0; i < records.Length; i++)
            {
                ApolloTracingResolverRecord record =
                    records[i];

                resolvers[i] = new OrderedDictionary
                {
                    {
                        ApolloTracingResultKeys.Path,
                        record.Path
                    },
                    {
                        ApolloTracingResultKeys.ParentType,
                        record.ParentType
                    },
                    {
                        ApolloTracingResultKeys.FieldName,
                        record.FieldName
                    },
                    {
                        ApolloTracingResultKeys.ReturnType,
                        record.ReturnType
                    },
                    {
                        ApolloTracingResultKeys.StartOffset,
                        record.StartTimestamp - _startTimestamp
                    },
                    {
                        ApolloTracingResultKeys.Duration,
                        record.EndTimestamp - record.StartTimestamp
                    }
                };
            }

            return resolvers;
        }
    }
}
