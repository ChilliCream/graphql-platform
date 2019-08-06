using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DiagnosticAdapter;

namespace GreenDonut
{
    public class TestListener
    {
        public readonly ConcurrentQueue<KeyValuePair<string, string>>
            BatchErrors =
                new ConcurrentQueue<KeyValuePair<string, string>>();
        public readonly ConcurrentQueue<string> BatchKeys =
            new ConcurrentQueue<string>();
        public readonly ConcurrentQueue<KeyValuePair<string, string>>
            BatchEntries =
                new ConcurrentQueue<KeyValuePair<string, string>>();
        public readonly ConcurrentQueue<KeyValuePair<string, Task<string>>>
            CachedEntries =
                new ConcurrentQueue<KeyValuePair<string, Task<string>>>();
        public readonly ConcurrentQueue<KeyValuePair<string, string>>
            Entries =
                new ConcurrentQueue<KeyValuePair<string, string>>();
        public readonly ConcurrentQueue<KeyValuePair<string, string>>
            Errors =
                new ConcurrentQueue<KeyValuePair<string, string>>();
        public readonly ConcurrentQueue<string> Keys =
            new ConcurrentQueue<string>();

        [DiagnosticName("GreenDonut.BatchError")]
        public void OnBatchError(
            IReadOnlyList<string> keys,
            Exception exception)
        {
            BatchErrors.Enqueue(
                new KeyValuePair<string, string>(
                    keys.FirstOrDefault(),
                    exception.Message));
        }

        [DiagnosticName("GreenDonut.CachedValue")]
        public void OnCachedValue(string key, Task<string> value)
        {
            CachedEntries.Enqueue(
                new KeyValuePair<string, Task<string>>(key, value));
        }

        [DiagnosticName("GreenDonut.Error")]
        public void OnError(string key, Exception exception)
        {
            Errors.Enqueue(
                new KeyValuePair<string, string>(key, exception.Message));
        }

        [DiagnosticName("GreenDonut.ExecuteBatchRequest")]
        public void OnExecuteBatchRequest() { }

        [DiagnosticName("GreenDonut.ExecuteBatchRequest.Start")]
        public void OnExecuteBatchRequestStart(
            IReadOnlyList<string> keys)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                BatchKeys.Enqueue(keys[i]);
            }
        }

        [DiagnosticName("GreenDonut.ExecuteBatchRequest.Stop")]
        public void OnExecuteBatchRequestStop(
            IReadOnlyList<string> keys,
            IReadOnlyList<string> values)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                BatchEntries.Enqueue(
                    new KeyValuePair<string, string>(keys[i], values[i]));
            }
        }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest")]
        public void OnExecuteSingleRequest() { }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest.Start")]
        public void OnExecuteSingleRequestStart(string key)
        {
            Keys.Enqueue(key);
        }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest.Stop")]
        public void OnExecuteSingleRequestStop(
            string key,
            IReadOnlyCollection<string> values)
        {
            Entries.Enqueue(new KeyValuePair<string, string>(key,
                values.FirstOrDefault()));
        }
    }
}
