using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Http;

namespace HotChocolate.Utilities.Introspection;

internal class FeatureInspector
{
    private readonly SchemaFeatures _features = new();
    private readonly GraphQLHttpClient _client;
    private readonly IntrospectionOptions _options;
    private readonly CancellationToken _cancellationToken;

    private FeatureInspector(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken)
    {
        _client = client;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    private Task InspectAsync()
        => Task.WhenAll(
            InspectArgumentDeprecationAsync(),
            InspectDirectiveTypeAsync());

    private async Task InspectArgumentDeprecationAsync()
    {
        /*
        {
            "data": {
                "__type": {
                    "fields": [
                        {
                            "name": "args", 
                            "args": [
                                {
                                    "name": "includeDeprecated" # <--- we are looking for this!
                                }
                            ]
                        }
                    ]
                }
            }
        }
        */
        
        var request = IntrospectionQueryHelper.CreateInspectArgumentDeprecationRequest(_options);

        using var response = await _client.SendAsync(request, _cancellationToken).ConfigureAwait(false);
        using var result = await response.ReadAsResultAsync(_cancellationToken).ConfigureAwait(false);

        if (result.Data.ValueKind is JsonValueKind.Object && 
            result.Data.TryGetProperty("__type", out var type) &&
            type.TryGetProperty("fields", out var fields))
        {
            foreach (var field in fields.EnumerateArray())
            {
                if (!field.TryGetProperty("name", out var fieldName) || 
                    fieldName.ValueKind is not JsonValueKind.String)
                {
                    return;
                }

                if (fieldName.GetString().EqualsOrdinal("args"))
                {
                    if(!field.TryGetProperty("args", out var args) ||
                        args.ValueKind is not JsonValueKind.Array)
                    {
                        return;
                    }

                    foreach (var arg in args.EnumerateArray())
                    {
                        if (arg.TryGetProperty("name", out var argName) &&
                            argName.ValueKind is JsonValueKind.String &&
                            argName.GetString().EqualsOrdinal("includeDeprecated"))
                        {
                            _features.HasArgumentDeprecationSupport = true;
                        }
                    }
                }
            }
        } 
        
    }

    private async Task InspectDirectiveTypeAsync()
    {
        /*
        {
            "data": {
                "__type": {
                    "fields": [
                        {
                            "name": "locations"         # <--- we are looking for this!
                        },
                        {
                            "name": "isRepeatable"      # <--- and we are looking for this!
                        }
                    ]
                }
            }
        }
        */
        
        var request = IntrospectionQueryHelper.CreateInspectDirectiveTypeRequest(_options);

        using var response = await _client.SendAsync(request, _cancellationToken).ConfigureAwait(false);
        using var result = await response.ReadAsResultAsync(_cancellationToken).ConfigureAwait(false);

        if (result.Data.ValueKind is JsonValueKind.Object &&
            result.Data.TryGetProperty("__type", out var type) &&
            type.TryGetProperty("fields", out var fields))
        {
            var locations = false;
            var isRepeatable = false;
            
            foreach (var field in fields.EnumerateArray())
            {
                if (!field.TryGetProperty("name", out var fieldName) || 
                    fieldName.ValueKind is not JsonValueKind.String)
                {
                    return;
                }

                var fieldNameString = fieldName.GetString();
                
                if (fieldNameString.EqualsOrdinal("locations"))
                {
                    locations = true;
                    _features.HasDirectiveLocations = true;
                }
                
                if (fieldNameString.EqualsOrdinal("isRepeatable"))
                {
                    isRepeatable = true;
                    _features.HasRepeatableDirectives = true;
                }

                if (locations && isRepeatable)
                {
                    return;
                }
            }
        }
    }
}