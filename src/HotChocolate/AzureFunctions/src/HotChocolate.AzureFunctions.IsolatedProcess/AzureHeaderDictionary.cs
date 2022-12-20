using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

internal sealed class AzureHeaderDictionary : IHeaderDictionary
{
    private readonly HttpResponse _response;
    private readonly HttpResponseData _responseData;

    public AzureHeaderDictionary(HttpResponse response, HttpResponseData responseData)
    {
        _response = response;
        _responseData = responseData;
    }

    public void Add(KeyValuePair<string, StringValues> item)
    {
        _response.Headers.Add(item.Key, item.Value);
        _responseData.Headers.Add(item.Key, (IEnumerable<string>)item.Value);
    }

    public void Clear()
    {
        _response.Headers.Clear();
        _responseData.Headers.Clear();
    }

    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        var success = _response.Headers.Remove(item);
        _responseData.Headers.Remove(item.Key);
        return success;
    }

    public int Count => _response.Headers.Count;

    public bool IsReadOnly => _response.Headers.IsReadOnly;

    public void Add(string key, StringValues value)
    {
        _response.Headers.Add(key, value);
        _responseData.Headers.Add(key, (IEnumerable<string>)value);
    }

    public bool ContainsKey(string key)
        => _response.Headers.ContainsKey(key);

    public bool Remove(string key)
    {
        var success = _response.Headers.Remove(key);
        _responseData.Headers.Remove(key);
        return success;
    }

    public bool TryGetValue(string key, out StringValues value)
        => _response.Headers.TryGetValue(key, out value);

    public StringValues this[string key]
    {
        get => _response.Headers[key];
        set
        {
            _response.Headers[key] = value;
            _responseData.Headers.Add(key, (IEnumerable<string>)value);
        }
    }

    public long? ContentLength
    {
        get => _response.Headers.ContentLength;
        set
        {
            _response.Headers.ContentLength = value;
            _responseData.Headers.Add(
                HeaderNames.ContentLength,
                (string?)_response.Headers[HeaderNames.ContentLength]);
        }
    }

    public ICollection<string> Keys => _response.Headers.Keys;

    public ICollection<StringValues> Values => _response.Headers.Values;

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        => _response.Headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
