using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GreenDonut
{
    internal static class DiagnosticEvents
    {
        private const string _diagnosticSourceName = "GreenDonut";
        private const string _batchActivityName = _diagnosticSourceName + ".ExecuteBatchRequest";
        private const string _batchErrorEventName = _diagnosticSourceName + ".BatchError";
        private const string _cachedValueEventName = _diagnosticSourceName + ".CachedValue";
        private const string _errorEventName = _diagnosticSourceName + ".Error";

        private static readonly DiagnosticSource _source = new DiagnosticListener(
            _diagnosticSourceName);

        public static void ReceivedBatchError<TKey>(IReadOnlyList<TKey> keys, Exception exception)
            where TKey : notnull
        {
            var payload = new
            {
                exception,
                keys
            };

            if (_source.IsEnabled(_batchErrorEventName, payload))
            {
                _source.Write(_batchErrorEventName, payload);
            }
        }

        public static void ReceivedError<TKey>(TKey key, Exception exception)
            where TKey : notnull
        {
            var payload = new
            {
                exception,
                key
            };

            if (_source.IsEnabled(_errorEventName, payload))
            {
                _source.Write(_errorEventName, payload);
            }
        }

        public static void ReceivedValueFromCache<TKey, TValue>(
            TKey key,
            object cacheKey,
            Task<TValue> value)
                where TKey : notnull
        {
            var payload = new
            {
                cacheKey,
                key,
                value
            };

            if (_source.IsEnabled(_cachedValueEventName, payload))
            {
                _source.Write(_cachedValueEventName, payload);
            }
        }

        public static Activity? StartBatching<TKey>(IReadOnlyList<TKey> keys)
            where TKey : notnull
        {
            var payload = new
            {
                keys
            };

            if (_source.IsEnabled(_batchActivityName, payload))
            {
                var activity = new Activity(_batchActivityName);

                _source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void StopBatching<TKey, TValue>(
            Activity? activity,
            IReadOnlyList<TKey> keys,
            IReadOnlyCollection<TValue> values)
                where TKey : notnull
        {
            if (activity != null)
            {
                var payload = new
                {
                    keys,
                    values
                };

                if (_source.IsEnabled(_batchActivityName, payload))
                {
                    _source.StopActivity(activity, payload);
                }
            }
        }
    }
}
