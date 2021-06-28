using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Options;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingDiagnosticEventListener : DiagnosticEventListener
    {
        private const string _extensionKey = "tracing";
        private readonly TracingPreference _tracingPreference;
        private readonly ITimestampProvider _timestampProvider;

        public ApolloTracingDiagnosticEventListener(
            TracingPreference tracingPreference = TracingPreference.OnDemand,
            ITimestampProvider? timestampProvider = null)
        {
            _tracingPreference = tracingPreference;
            _timestampProvider = timestampProvider ?? new DefaultTimestampProvider();
        }

        public override bool EnableResolveFieldValue => true;

        public override IActivityScope ExecuteRequest(IRequestContext context)
        {
            if (IsEnabled(context.ContextData))
            {
                DateTime startTime = _timestampProvider.UtcNow();

                ApolloTracingResultBuilder builder = CreateBuilder(context.ContextData);

                builder.SetRequestStartTime(
                    startTime,
                    _timestampProvider.NowInNanoseconds());

                return new RequestScope(context, startTime, builder, _timestampProvider);
            }
            return EmptyScope;
        }

        public override IActivityScope ParseDocument(IRequestContext context)
        {
            return TryGetBuilder(context.ContextData, out ApolloTracingResultBuilder? builder)
                ? new ParseDocumentScope(builder, _timestampProvider)
                : EmptyScope;
        }

        public override IActivityScope ValidateDocument(IRequestContext context)
        {
            return TryGetBuilder(context.ContextData, out ApolloTracingResultBuilder? builder)
                ? new ValidateDocumentScope(builder, _timestampProvider)
                : EmptyScope;
        }

        public override IActivityScope ResolveFieldValue(IMiddlewareContext context)
        {
            return TryGetBuilder(context.ContextData, out ApolloTracingResultBuilder? builder)
                ? new ResolveFieldValueScope(context, builder, _timestampProvider)
                : EmptyScope;
        }

        private static ApolloTracingResultBuilder CreateBuilder(
            IDictionary<string, object?> contextData)
        {
            var builder = new ApolloTracingResultBuilder();
            contextData[nameof(ApolloTracingResultBuilder)] = builder;
            return builder;
        }

        private static bool TryGetBuilder(
            IDictionary<string, object?> contextData,
            [NotNullWhen(true)] out ApolloTracingResultBuilder? builder)
        {
            if (contextData.TryGetValue(nameof(ApolloTracingResultBuilder), out object? value) &&
                value is ApolloTracingResultBuilder b)
            {
                builder = b;
                return true;
            }

            builder = null;
            return false;
        }

        private bool IsEnabled(IDictionary<string, object?> contextData)
        {
            return (_tracingPreference == TracingPreference.Always ||
                (_tracingPreference == TracingPreference.OnDemand &&
                    contextData.ContainsKey(WellKnownContextData.EnableTracing)));
        }

        private class RequestScope : IActivityScope
        {
            private readonly IRequestContext _context;
            private readonly DateTime _startTime;
            private readonly ApolloTracingResultBuilder _builder;
            private readonly ITimestampProvider _timestampProvider;
            private bool _disposed;

            public RequestScope(
                IRequestContext context,
                DateTime startTime,
                ApolloTracingResultBuilder builder,
                ITimestampProvider timestampProvider)
            {
                _context = context;
                _startTime = startTime;
                _builder = builder;
                _timestampProvider = timestampProvider;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    DateTime endTime = _timestampProvider.UtcNow();
                    _builder.SetRequestDuration(endTime - _startTime);

                    if (_context.Result is IReadOnlyQueryResult queryResult)
                    {
                        _context.Result = QueryResultBuilder.FromResult(queryResult)
                            .AddExtension(_extensionKey, _builder.Build())
                            .Create();
                    }
                    _disposed = true;
                }
            }
        }

        private class ParseDocumentScope : IActivityScope
        {
            private readonly ApolloTracingResultBuilder _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;

            public ParseDocumentScope(
                ApolloTracingResultBuilder builder,
                ITimestampProvider timestampProvider)
            {
                _builder = builder;
                _timestampProvider = timestampProvider;
                _startTimestamp = timestampProvider.NowInNanoseconds();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _builder.SetParsingResult(
                        _startTimestamp,
                        _timestampProvider.NowInNanoseconds());
                    _disposed = true;
                }
            }
        }

        private class ValidateDocumentScope : IActivityScope
        {
            private readonly ApolloTracingResultBuilder _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;

            public ValidateDocumentScope(
                ApolloTracingResultBuilder builder,
                ITimestampProvider timestampProvider)
            {
                _builder = builder;
                _timestampProvider = timestampProvider;
                _startTimestamp = timestampProvider.NowInNanoseconds();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _builder.SetValidationResult(
                        _startTimestamp,
                        _timestampProvider.NowInNanoseconds());
                    _disposed = true;
                }
            }
        }

        private class ResolveFieldValueScope : IActivityScope
        {
            private readonly IMiddlewareContext _context;
            private readonly ApolloTracingResultBuilder _builder;
            private readonly ITimestampProvider _timestampProvider;
            private readonly long _startTimestamp;
            private bool _disposed;

            public ResolveFieldValueScope(
                IMiddlewareContext context,
                ApolloTracingResultBuilder builder,
                ITimestampProvider timestampProvider)
            {
                _context = context;
                _builder = builder;
                _timestampProvider = timestampProvider;
                _startTimestamp = timestampProvider.NowInNanoseconds();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    var stopTimestamp = _timestampProvider.NowInNanoseconds();

                    _builder.AddResolverResult(
                        new ApolloTracingResolverRecord(
                            _context,
                            _startTimestamp,
                            stopTimestamp));
                    _disposed = true;
                }
            }
        }
    }

    public interface ITimestampProvider
    {
        DateTime UtcNow();

        long NowInNanoseconds();
    }

    public class DefaultTimestampProvider : ITimestampProvider
    {
        public DateTime UtcNow() => DateTime.UtcNow;

        public long NowInNanoseconds() => Timestamp.GetNowInNanoseconds();
    }
}
