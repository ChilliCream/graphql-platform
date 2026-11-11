using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elastic.Clients.Esql;

namespace HotChocolate.Data.ElasticSearch;

public sealed record TestClient(EsqlClient Esql, string Index);

public class TestBase
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly HttpClient _http;

    protected TestBase(ElasticsearchResource resource)
    {
        _http = new HttpClient { BaseAddress = resource.Uri };
        Client = new TestClient(
            new EsqlClient(new EsqlClientSettings(resource.Uri) { JsonSerializerOptions = s_options }),
            DefaultIndexName);
    }

    protected TestClient Client { get; }

    protected string DefaultIndexName { get; } = $"{Guid.NewGuid():N}";

    protected EsqlClient CreateClient(JsonSerializerOptions options)
        => new(new EsqlClientSettings(_http.BaseAddress!) { JsonSerializerOptions = options });

    protected async Task IndexDocuments<T>(IEnumerable<T> data, JsonSerializerOptions? options = null)
        where T : class
    {
        await _http.DeleteAsync("/_all", TestContext.Current.CancellationToken);

        var enumsAsText = options?.Converters.Any(c => c is JsonStringEnumConverter) == true;
        var naming = (options ?? s_options).PropertyNamingPolicy;
        var mapping = new JsonObject
        {
            ["mappings"] = new JsonObject { ["properties"] = Properties(typeof(T), naming, enumsAsText) }
        };
        using var created = await _http.PutAsync(
            "/" + DefaultIndexName,
            new StringContent(mapping.ToJsonString(), Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        created.EnsureSuccessStatusCode();

        var bulk = new StringBuilder();

        foreach (var element in data)
        {
            bulk.Append("{\"index\":{}}\n").Append(JsonSerializer.Serialize(element, options ?? s_options)).Append('\n');
        }

        using var indexed = await _http.PostAsync(
            $"/{DefaultIndexName}/_bulk?refresh=true",
            new StringContent(bulk.ToString(), Encoding.UTF8, "application/x-ndjson"),
            TestContext.Current.CancellationToken);
        indexed.EnsureSuccessStatusCode();

        // a bulk request succeeds even when some of its documents are rejected.
        var response = JsonNode.Parse(
            await indexed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        if (response["errors"]?.GetValue<bool>() == true)
        {
            throw new InvalidOperationException("Some documents were not indexed: " + response.ToJsonString());
        }
    }

    private static JsonObject Properties(Type type, JsonNamingPolicy? naming, bool enumsAsText)
    {
        var properties = new JsonObject();

        foreach (var property in type.GetProperties())
        {
            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? naming?.ConvertName(property.Name)
                ?? property.Name;
            properties[name] = Mapping(property.PropertyType, property, naming, enumsAsText);
        }

        return properties;
    }

    private static JsonObject Mapping(Type type, PropertyInfo property, JsonNamingPolicy? naming, bool enumsAsText)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
        {
            return new JsonObject
            {
                ["type"] = enumsAsText || property.IsDefined(typeof(JsonConverterAttribute)) ? "keyword" : "integer"
            };
        }

        if (type == typeof(string)
            && property.GetCustomAttribute<ElasticSearchKeywordAttribute>() is { } keyword)
        {
            return new JsonObject
            {
                ["type"] = "text",
                ["fields"] = new JsonObject { [keyword.Subfield] = new JsonObject { ["type"] = "keyword" } }
            };
        }

        if (type == typeof(string) || type == typeof(Guid) || type == typeof(Uri))
        {
            return new JsonObject { ["type"] = "keyword" };
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return new JsonObject { ["type"] = "date" };
        }

        if (type == typeof(bool))
        {
            return new JsonObject { ["type"] = "boolean" };
        }

        if (type == typeof(int))
        {
            return new JsonObject { ["type"] = "integer" };
        }

        if (type == typeof(long))
        {
            return new JsonObject { ["type"] = "long" };
        }

        if (type == typeof(float))
        {
            return new JsonObject { ["type"] = "float" };
        }

        if (type == typeof(double))
        {
            return new JsonObject { ["type"] = "double" };
        }

        var element = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];

        if (element is not null)
        {
            return Mapping(element, property, naming, enumsAsText);
        }

        var mapping = new JsonObject { ["properties"] = Properties(type, naming, enumsAsText) };

        if (property.IsDefined(typeof(ElasticSearchNestedAttribute)))
        {
            mapping["type"] = "nested";
        }

        return mapping;
    }
}
