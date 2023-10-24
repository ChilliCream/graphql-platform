using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Utilities.Introspection;

internal static class IntrospectionQueryHelper
{
    private const string _resourceNamespace = "HotChocolate.Utilities.Introspection.Queries";
    private const string _argumentDeprecationQueryFile = "inspect_argument_deprecation.graphql";
    private const string _inspectDirectiveType = "inspect_directive_type.graphql";
    private const string _inspectDirectives = "inspect_directives.graphql";
    private const string _inspectSchema = "inspect_schema.graphql";
    private const string _introspectionQueryFile = "introspection_phase_2.graphql";
    private const string _onOperation = "onOperation";
    private const string _onFragment = "onFragment";
    private const string _onField = "onField";
    private const string _directivesField = "directives";
    private const string _operationName = "IntrospectionQuery";

    public static GraphQLHttpRequest CreateInspectArgumentDeprecationRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetArgumentDeprecationQuery()), options);

    public static GraphQLHttpRequest CreateInspectDirectiveTypeRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectDirectiveTypeQuery()), options);

    public static GraphQLHttpRequest CreateInspectDirectivesRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectDirectivesQuery()), options);

    public static GraphQLHttpRequest CreateInspectSchemaRequest(IntrospectionOptions options)
        => CreateRequest(CreateOperation(GetInspectSchemaQuery()), options);

    private static OperationRequest CreateOperation(string document)
        => new(document, operationName: _operationName);

    private static GraphQLHttpRequest CreateRequest(OperationRequest operation, IntrospectionOptions options)
        => new(operation)
        {
            Method = options.Method,
            Uri = options.Uri,
            OnMessageCreated = options.OnMessageCreated
        };

    public static OperationRequest CreateIntrospectionQuery(SchemaFeatures features)
    {
        var document = CreateIntrospectionQueryDocument(features);
        var sourceText = document.Print(false);
        return new(sourceText, operationName: "IntrospectionQuery");
    }

    private static DocumentNode CreateIntrospectionQueryDocument(SchemaFeatures features)
    {
        var query = Utf8GraphQLParser.Parse(GetIntrospectionQuery());

        var operation =
            query.Definitions.OfType<OperationDefinitionNode>().First();

        var schema =
            operation.SelectionSet.Selections.OfType<FieldNode>().First();

        if (schema.SelectionSet is null)
        {
            throw new IntrospectionException();
        }

        var directives =
            schema.SelectionSet.Selections.OfType<FieldNode>().First(
                t =>
                    t.Name.Value.Equals(_directivesField, StringComparison.Ordinal));

        if (directives.SelectionSet is null)
        {
            throw new IntrospectionException();
        }

        var selections = directives.SelectionSet.Selections.ToList();
        AddDirectiveFeatures(features, selections);

        var newField = directives.WithSelectionSet(
            directives.SelectionSet.WithSelections(selections));

        selections = schema.SelectionSet.Selections.ToList();
        RemoveSubscriptionIfNotSupported(features, selections);

        selections.Remove(directives);
        selections.Add(newField);

        newField = schema.WithSelectionSet(
            schema.SelectionSet.WithSelections(selections));

        selections = operation.SelectionSet.Selections.ToList();
        selections.Remove(schema);
        selections.Add(newField);

        var newOp = operation.WithSelectionSet(
            operation.SelectionSet.WithSelections(selections));

        var definitions = query.Definitions.ToList();
        definitions.Remove(operation);
        definitions.Insert(0, newOp);

        return query.WithDefinitions(definitions);
    }

    private static void AddDirectiveFeatures(
        SchemaFeatures features,
        ICollection<ISelectionNode> selections)
    {
        if (features.HasDirectiveLocations)
        {
            selections.Add(CreateField(LegacySchemaFeatures.Locations));
        }
        else
        {
            selections.Add(CreateField(_onField));
            selections.Add(CreateField(_onFragment));
            selections.Add(CreateField(_onOperation));
        }

        if (features.HasRepeatableDirectives)
        {
            selections.Add(CreateField(LegacySchemaFeatures.IsRepeatable));
        }
    }

    private static void RemoveSubscriptionIfNotSupported(
        SchemaFeatures features,
        ICollection<ISelectionNode> selections)
    {
        if (!features.HasSubscriptionSupport)
        {
            var subscriptionField = selections.OfType<FieldNode>()
                .First(
                    t => t.Name.Value.Equals(
                        LegacySchemaFeatures.SubscriptionType,
                        StringComparison.Ordinal));
            selections.Remove(subscriptionField);
        }
    }

    private static FieldNode CreateField(string name) =>
        new(
            null,
            new NameNode(name),
            null,
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

    private static string GetArgumentDeprecationQuery() => GetQueryFile(_argumentDeprecationQueryFile);

    private static string GetInspectDirectiveTypeQuery() => GetQueryFile(_inspectDirectiveType);

    private static string GetInspectDirectivesQuery() => GetQueryFile(_inspectDirectives);

    private static string GetInspectSchemaQuery() => GetQueryFile(_inspectSchema);

    private static string GetIntrospectionQuery() => GetQueryFile(_introspectionQueryFile);

    private static string GetQueryFile(string fileName)
    {
#pragma warning disable CS8600
        var stream = typeof(IntrospectionClient).Assembly
            .GetManifestResourceStream($"{_resourceNamespace}.{fileName}");
#pragma warning restore CS8600

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

public struct IntrospectionOptions : IEquatable<IntrospectionOptions>
{
    public IntrospectionOptions()
    {
        Method = GraphQLHttpMethod.Post;
        Uri = null;
        OnMessageCreated = null;
        TypeDepth = 6;
    }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public GraphQLHttpMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the GraphQL request <see cref="Uri"/>.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets a hook that can alter the <see cref="HttpRequestMessage"/> before it is sent.
    /// </summary>
    public OnHttpRequestMessageCreated? OnMessageCreated { get; set; }

    public int TypeDepth { get; set; }

    internal void Validate()
    {
        
    }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(IntrospectionOptions left, IntrospectionOptions right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(IntrospectionOptions left, IntrospectionOptions right)
    {
        return !(left == right);
    }

    public bool Equals(IntrospectionOptions other)
    {
        throw new NotImplementedException();
    }
}