using System.Text;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using static HotChocolate.Utilities.Introspection.IntrospectionQueryBuilder;

namespace HotChocolate.Utilities.Introspection;

internal static class IntrospectionQueryHelper
{
    private const string ResourceNamespace = "HotChocolate.Utilities.Introspection.Queries";
    private const string ArgumentDeprecationQueryFile = "inspect_argument_deprecation.graphql";
    private const string InspectDirectiveType = "inspect_directive_type.graphql";
    private const string InspectDirectives = "inspect_directives.graphql";
    private const string InspectSchema = "inspect_schema.graphql";
    private const string OperationName = "IntrospectionQuery";

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
        => new(document, operationName: OperationName);

    private static GraphQLHttpRequest CreateRequest(OperationRequest operation, IntrospectionOptions options)
        => new(operation)
        {
            Method = options.Method,
            Uri = options.Uri,
            OnMessageCreated = options.OnMessageCreated,
        };

    private static string GetArgumentDeprecationQuery() => GetQueryFile(ArgumentDeprecationQueryFile);

    private static string GetInspectDirectiveTypeQuery() => GetQueryFile(InspectDirectiveType);

    private static string GetInspectDirectivesQuery() => GetQueryFile(InspectDirectives);

    private static string GetInspectSchemaQuery() => GetQueryFile(InspectSchema);

    private static string GetQueryFile(string fileName)
    {
        var stream = typeof(IntrospectionClient).Assembly
            .GetManifestResourceStream($"{ResourceNamespace}.{fileName}");

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
