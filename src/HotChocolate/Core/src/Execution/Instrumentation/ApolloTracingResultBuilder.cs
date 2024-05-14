using System;
using System.Collections.Concurrent;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Instrumentation.ApolloTracingResultKeys;

namespace HotChocolate.Execution.Instrumentation;

internal class ApolloTracingResultBuilder
{
    private const int _apolloTracingVersion = 1;
    private const long _ticksToNanosecondsMultiplier = 100;
    private readonly ConcurrentQueue<ApolloTracingResolverRecord> _resolverRecords =
        new ConcurrentQueue<ApolloTracingResolverRecord>();
    private TimeSpan _duration;
    private ObjectResult? _parsingResult;
    private DateTimeOffset _startTime;
    private long _startTimestamp;
    private ObjectResult? _validationResult;

    public void SetRequestStartTime(
        DateTimeOffset startTime,
        long startTimestamp)
    {
        _startTime = startTime;
        _startTimestamp = startTimestamp;
    }

    public void SetParsingResult(long startTimestamp, long endTimestamp)
    {
        _parsingResult = new ObjectResult();
        _parsingResult.EnsureCapacity(2);
        _parsingResult.SetValueUnsafe(0, StartOffset, startTimestamp - _startTimestamp);
        _parsingResult.SetValueUnsafe(1, Duration, endTimestamp - startTimestamp);
    }

    public void SetValidationResult(long startTimestamp, long endTimestamp)
    {
        _validationResult = new ObjectResult();
        _validationResult.EnsureCapacity(2);
        _validationResult.SetValueUnsafe(0, StartOffset, startTimestamp - _startTimestamp);
        _validationResult.SetValueUnsafe(1, Duration, endTimestamp - startTimestamp);
    }

    public void AddResolverResult(ApolloTracingResolverRecord record)
    {
        _resolverRecords.Enqueue(record);
    }

    public void SetRequestDuration(TimeSpan duration)
    {
        _duration = duration;
    }

    public ObjectResult Build()
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

        var result = new ObjectResult();
        result.EnsureCapacity(1);
        result.SetValueUnsafe(0, ApolloTracingResultKeys.Resolvers, BuildResolverResults());

        var details = new ObjectResult();
        details.EnsureCapacity(7);
        details.SetValueUnsafe(0, ApolloTracingResultKeys.Version, _apolloTracingVersion);
        details.SetValueUnsafe(1, StartTime, _startTime.ToRfc3339DateTimeString());
        details.SetValueUnsafe(2, EndTime, _startTime.Add(_duration).ToRfc3339DateTimeString());
        details.SetValueUnsafe(3, Duration, _duration.Ticks * _ticksToNanosecondsMultiplier);
        details.SetValueUnsafe(4, Parsing, _parsingResult);
        details.SetValueUnsafe(5, ApolloTracingResultKeys.Validation, _validationResult);
        details.SetValueUnsafe(6, ApolloTracingResultKeys.Execution, result);
        return details;
    }

    private ObjectResult[] BuildResolverResults()
    {
        var i = 0;
        var results = new ObjectResult[_resolverRecords.Count];

        foreach (var record in _resolverRecords)
        {
            var result = new ObjectResult();
            result.EnsureCapacity(6);
            result.SetValueUnsafe(0, ApolloTracingResultKeys.Path, record.Path);
            result.SetValueUnsafe(1, ParentType, record.ParentType);
            result.SetValueUnsafe(2, FieldName, record.FieldName);
            result.SetValueUnsafe(3, ReturnType, record.ReturnType);
            result.SetValueUnsafe(4, StartOffset, record.StartTimestamp - _startTimestamp);
            result.SetValueUnsafe(5, Duration, record.EndTimestamp - record.StartTimestamp);
            results[i++] = result;
        }

        return results;
    }
}
