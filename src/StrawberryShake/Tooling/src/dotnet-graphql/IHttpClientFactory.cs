using System;
using System.Collections.Generic;
using System.Net.Http;

namespace StrawberryShake.Tools;

public interface IHttpClientFactory
{
    HttpClient Create(
        Uri uri,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders);
}