using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public partial class QueryRequestBuilder
    {
        private class EmptyDictionary
            : IReadOnlyDictionary<string, object>
        {
            private EmptyDictionary() { }
            private readonly Dictionary<string, object> _innerDict =
                new Dictionary<string, object>();

            public object this[string key] => _innerDict[key];

            public IEnumerable<string> Keys => _innerDict.Keys;

            public IEnumerable<object> Values => _innerDict.Values;

            public int Count => _innerDict.Count;

            public bool ContainsKey(string key) => _innerDict.ContainsKey(key);

            public bool TryGetValue(string key, out object value) =>
                _innerDict.TryGetValue(key, out value);

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
                _innerDict.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public static EmptyDictionary Instance { get; } =
                new EmptyDictionary();
        }
    }
}
