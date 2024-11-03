using System.Text;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using static HotChocolate.Utilities.Introspection.IntrospectionQueryBuilder;

namespace HotChocolate.Utilities.Introspection;

internal static class IntrospectionQueryHelper
{
    private const string _resourceNamespace = "HotChocolate.Utilities.Introspection.Queries";
    private const string _argumentDeprecationQueryFile = "inspect_argument_deprecation.graphql";
    private const string _inspectDirectiveType = "inspect_directive_type.graphql";
    private const string _inspectDirectives = "inspect_directives.graphql";
    private const string _inspectSchema = "inspect_schema.graphql";
    private const string _operationName = "IntrospectionQuery";

    public static GraphQLHttpRequest CreateInspectArgumentDeprecationRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetArgumentDeprecationQuery()), options);

    public static GraphQLHttpRequest CreateInspectDirectiveTypeRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectDirectiveTypeQuery()), options);

    public static GraphQLHttpRequest CreateInspectDirectivesRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectDirectivesQuery()), options);

    public static GraphQLHttpRequest CreateInspectSchemaRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectSchemaQuery()), options);

    public static GraphQLHttpRequest CreateIntrospectionRequest(ServerCapabilities features, IntrospectionOptions options)
        => CreateRequest(CreateOperation(Build(features, options).Print(false)), options);

    private static OperationRequest CreateOperation(string document)
        => new(document, operationName: _operationName);

    private static GraphQLHttpRequest CreateRequest(OperationRequest operation, IntrospectionOptions options)
        => new(operation)
        {
            Method = options.Method,
            Uri = options.Uri,
            OnMessageCreated = options.OnMessageCreated,
        };

    private static string GetArgumentDeprecationQuery() => GetQueryFile(_argumentDeprecationQueryFile);

    private static string GetInspectDirectiveTypeQuery() => GetQueryFile(_inspectDirectiveType);

    private static string GetInspectDirectivesQuery() => GetQueryFile(_inspectDirectives);

    private static string GetInspectSchemaQuery() => GetQueryFile(_inspectSchema);

    private static string GetQueryFile(string fileName)
    {
        var stream = typeof(IntrospectionClient).Assembly
            .GetManifestResourceStream($"{_resourceNamespace}.{fileName}");

        if (stream is not null)
        {
            try
            {
                var buffer = new byte[stream.Length];

                if (stream.Read(buffer, 0, buffer.Length) > 0)
                {
                    return Encoding.UTF8.GetString(buffer);
                }
            }
            finally
            {
                stream.Dispose();
            }
        }

        throw new IntrospectionException("Could not find query file: " + fileName);
    }
}
