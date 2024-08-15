namespace StrawberryShake.Tools;

public interface IHttpClientFactory
{
    HttpClient Create(
        Uri uri,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders);
}
