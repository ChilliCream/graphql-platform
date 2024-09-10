using System.Text.Json;
using HotChocolate.Transport.Http;
using static HotChocolate.Utilities.Introspection.IntrospectionQueryHelper;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// This helper class issues requests to determine what capabilities a GraphQL server has.
/// </summary>
internal sealed class CapabilityInspector
{
    private readonly ServerCapabilities _features = new();
    private readonly GraphQLHttpClient _client;
    private readonly IntrospectionOptions _options;
    private readonly CancellationToken _cancellationToken;

    private CapabilityInspector(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken)
    {
        _client = client;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public static async Task<ServerCapabilities> InspectAsync(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        var inspector = new CapabilityInspector(client, options, cancellationToken);
        await inspector.RunInspectionAsync().ConfigureAwait(false);
        return inspector._features;
    }

    private Task RunInspectionAsync()
        => Task.WhenAll(
            InspectArgumentDeprecationAsync(),
            InspectDirectiveTypeAsync(),
            InspectDirectivesAsync(),
            InspectSchemaAsync());

    private async Task InspectArgumentDeprecationAsync()
    {
        // Queries/inspect_argument_deprecation.graphql

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

        var request = CreateInspectArgumentDeprecationRequest(_options);

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
                    if (!field.TryGetProperty("args", out var args) ||
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
                            _features.HasArgumentDeprecation = true;
                        }
                    }
                }
            }
        }
    }

    private async Task InspectDirectiveTypeAsync()
    {
        // Queries/inspect_directive_type.graphql

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

        var request = CreateInspectDirectiveTypeRequest(_options);

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

    private async Task InspectDirectivesAsync()
    {
        // Queries/inspect_directives.graphql

        /*
        {
            "data": {
                "__schema": {
                    "directives": [
                        {
                            "name": "skip"
                        },
                        {
                            "name": "include"
                        },
                        {
                            "name": "defer"             # <--- we are looking for this!
                        },
                        {
                            "name": "stream"            # <--- or this!
                        },
                        {
                            "name": "deprecated"
                        }
                    ]
                }
            }
        }
        */

        var request = CreateInspectDirectivesRequest(_options);

        using var response = await _client.SendAsync(request, _cancellationToken).ConfigureAwait(false);
        using var result = await response.ReadAsResultAsync(_cancellationToken).ConfigureAwait(false);

        if (result.Data.ValueKind is JsonValueKind.Object &&
            result.Data.TryGetProperty("__schema", out var schema) &&
            schema.TryGetProperty("directives", out var directives))
        {
            var defer = false;
            var stream = false;

            foreach (var directive in directives.EnumerateArray())
            {
                if (!directive.TryGetProperty("name", out var directiveName) ||
                    directiveName.ValueKind is not JsonValueKind.String)
                {
                    return;
                }

                var directiveNameString = directiveName.GetString();

                if (directiveNameString.EqualsOrdinal("defer"))
                {
                    defer = true;
                    _features.HasDeferSupport = true;
                }

                if (directiveNameString.EqualsOrdinal("stream"))
                {
                    stream = true;
                    _features.HasStreamSupport = true;
                }

                if (defer && stream)
                {
                    return;
                }
            }
        }
    }

    private async Task InspectSchemaAsync()
    {
        // Queries/inspect_schema.graphql

        /*
        {
            "data": {
                "__type": {
                    "fields": [
                        {
                            "name": "description"               # <--- we are looking for this!
                        },
                        {
                            "name": "types"
                        },
                        {
                            "name": "queryType"
                        },
                        {
                            "name": "mutationType"
                        },
                        {
                            "name": "subscriptionType"          # <--- or this!
                        },
                        {
                            "name": "directives"
                        }
                    ]
                }
            }
        }
        */

        var request = CreateInspectSchemaRequest(_options);

        using var response = await _client.SendAsync(request, _cancellationToken).ConfigureAwait(false);
        using var result = await response.ReadAsResultAsync(_cancellationToken).ConfigureAwait(false);

        if (result.Data.ValueKind is JsonValueKind.Object &&
            result.Data.TryGetProperty("__type", out var type) &&
            type.TryGetProperty("fields", out var fields))
        {
            var description = false;
            var subscriptionType = false;

            foreach (var field in fields.EnumerateArray())
            {
                if (!field.TryGetProperty("name", out var fieldName) ||
                    fieldName.ValueKind is not JsonValueKind.String)
                {
                    return;
                }

                var fieldNameString = fieldName.GetString();

                if (fieldNameString.EqualsOrdinal("description"))
                {
                    description = true;
                    _features.HasSchemaDescription = true;
                }

                if (fieldNameString.EqualsOrdinal("subscriptionType"))
                {
                    subscriptionType = true;
                    _features.HasSubscriptionSupport = true;
                }

                if (description && subscriptionType)
                {
                    return;
                }
            }
        }
    }
}
