using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi;

internal static class OpenApiEndpointDefinitionExtensions
{
    private static readonly ResponseBodySelectionFinder s_responseBodySelectionFinder = new();

    public static OpenApiResponseBodySelection GetResponseBodySelection(
        this OpenApiEndpointDefinition endpoint,
        ISchemaDefinition schema)
    {
        var operation = endpoint.OperationDefinition;
        var rootType = schema.GetOperationType(operation.Operation);

        return FindResponseBody(operation.SelectionSet, rootType)
            ?? CreateDefaultResponseBody(operation.SelectionSet, rootType);
    }

    private static OpenApiResponseBodySelection CreateDefaultResponseBody(
        SelectionSetNode selectionSet,
        IOutputType? rootType)
    {
        var rootField = selectionSet.Selections.FirstOrDefault() as FieldNode
            ?? throw new InvalidOperationException("Expected to have a response field.");

        return new OpenApiResponseBodySelection(
            [rootField.Alias?.Value ?? rootField.Name.Value],
            rootField.SelectionSet,
            ResolveFieldType(rootField, rootType));
    }

    private static OpenApiResponseBodySelection? FindResponseBody(
        SelectionSetNode selectionSet,
        IOutputType? rootType)
    {
        var context = new ResponseBodySelectionFinderContext(rootType);
        s_responseBodySelectionFinder.Visit(selectionSet, context);
        return context.ResponseBodySelection;
    }

    private static IOutputType? ResolveFieldType(FieldNode field, IOutputType? declaringType)
    {
        if (declaringType?.NamedType() is IComplexTypeDefinition complexType
            && complexType.Fields.TryGetField(field.Name.Value, out var fieldDefinition))
        {
            return fieldDefinition.Type;
        }

        return null;
    }

    private sealed class ResponseBodySelectionFinder
        : SyntaxWalker<ResponseBodySelectionFinderContext>
    {
        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            ResponseBodySelectionFinderContext context)
        {
            var declaringType = context.Path.Count == 0
                ? context.RootType
                : context.Path[^1].FieldType;
            var fieldType = ResolveFieldType(node, declaringType);
            context.Path.Add(
                new ResponseBodyPathSegment(
                    node.Alias?.Value ?? node.Name.Value,
                    fieldType));

            if (node.Directives.Any(
                    d => d.Name.Value == WellKnownDirectiveNames.ResponseBody))
            {
                context.ResponseBodySelection = new OpenApiResponseBodySelection(
                    CreateResponseNamePath(context.Path),
                    node.SelectionSet,
                    fieldType);
                return Break;
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            ResponseBodySelectionFinderContext context)
        {
            context.Path.RemoveAt(context.Path.Count - 1);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            ResponseBodySelectionFinderContext context)
            => node.TypeCondition is null ? Continue : Skip;

        private static ImmutableArray<string> CreateResponseNamePath(
            List<ResponseBodyPathSegment> path)
        {
            var responseNamePath = new string[path.Count];
            for (var i = 0; i < path.Count; i++)
            {
                responseNamePath[i] = path[i].ResponseName;
            }

            return ImmutableCollectionsMarshal.AsImmutableArray(responseNamePath);
        }
    }

    private sealed class ResponseBodySelectionFinderContext(IOutputType? rootType)
    {
        public IOutputType? RootType { get; } = rootType;

        public List<ResponseBodyPathSegment> Path { get; } = [];

        public OpenApiResponseBodySelection? ResponseBodySelection { get; set; }
    }

    private readonly record struct ResponseBodyPathSegment(
        string ResponseName,
        IOutputType? FieldType);
}

internal sealed record OpenApiResponseBodySelection(
    ImmutableArray<string> ResponseNamePath,
    SelectionSetNode? SelectionSet,
    IOutputType? FieldType);
