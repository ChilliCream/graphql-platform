using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Validation;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.CostAnalysis.Utilities;

internal static class CostAnalyzerUtilities
{
    public static void ValidateRequireOneSlicingArgument(
        Operation operation,
        DocumentNode sourceDocument,
        OperationDocumentId documentId,
        IFeatureCollection features,
        ObjectPool<DocumentValidatorContext> contextPool)
    {
        var validatorContext = contextPool.Get();

        try
        {
            validatorContext.Initialize(
                operation.Schema,
                documentId,
                operation.Document,
                maxAllowedErrors: 1,
                maxLocationsPerError: 5,
                maxAllowedFragmentVisits: 1_000,
                features);

            var sourceOperation = sourceDocument.GetOperation(operation.Name);
            var sourceFields = SourceFieldIndex.Create(operation.Schema, sourceDocument, sourceOperation);
            new RequireOneSlicingArgumentVisitor(sourceFields).Visit(operation.Definition, validatorContext);
        }
        finally
        {
            validatorContext.Clear();
            contextPool.Return(validatorContext);
        }
    }

    public static void ValidateRequireOneSlicingArgument(
        this ListSizeDirective? listSizeDirective,
        FieldNode node,
        FieldNode sourceNode,
        IList<ISyntaxNode> path)
    {
        if (listSizeDirective?.RequireOneSlicingArgument ?? false)
        {
            var argumentCount = 0;
            var variableCount = 0;

            foreach (var argumentNode in node.Arguments)
            {
                if (listSizeDirective.SlicingArguments.Contains(argumentNode.Name.Value))
                {
                    if (argumentNode.Value.Kind == SyntaxKind.NullValue)
                    {
                        continue;
                    }

                    argumentCount++;

                    if (argumentNode.Value.Kind == SyntaxKind.Variable)
                    {
                        variableCount++;
                    }
                }
            }

            if (argumentCount > 0
                && argumentCount == variableCount
                && argumentCount <= listSizeDirective.SlicingArguments.Length)
            {
                return;
            }

            if (argumentCount != 1)
            {
                throw new GraphQLException(
                    ErrorHelper.ExactlyOneSlicingArgMustBeDefined(sourceNode, path));
            }
        }
    }

    private sealed class RequireOneSlicingArgumentVisitor(SourceFieldIndex sourceFields)
        : TypeDocumentValidatorVisitor
    {
        private readonly Stack<(ListSizeDirective? Directive, FieldNode Source)> _fields = [];

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            DocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out var type)
                && type.NamedType() is IComplexTypeDefinition declaringType
                && declaringType.Fields.TryGetField(node.Name.Value, out var field))
            {
                var listSizeDirective = field.Directives.FirstOrDefaultValue<ListSizeDirective>();
                var sourceNode = sourceFields.Find(node, declaringType.Name, context.Path) ?? node;

                if (node.SelectionSet is null)
                {
                    listSizeDirective.ValidateRequireOneSlicingArgument(node, sourceNode, context.Path);
                    return Skip;
                }

                _fields.Push((listSizeDirective, sourceNode));
                context.OutputFields.Push(field);
                context.Types.Push(field.Type);
                return Continue;
            }

            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            DocumentValidatorContext context)
        {
            var (listSizeDirective, sourceNode) = _fields.Pop();
            listSizeDirective.ValidateRequireOneSlicingArgument(node, sourceNode, context.Path);
            context.OutputFields.Pop();
            context.Types.Pop();
            return Continue;
        }
    }

    private sealed class SourceFieldIndex
    {
        private readonly Dictionary<FieldKey, FieldNode> _fields = [];
        private readonly Dictionary<FallbackFieldKey, FieldNode> _fallbackFields = [];

        private SourceFieldIndex()
        {
        }

        public static SourceFieldIndex Create(
            Schema schema,
            DocumentNode document,
            OperationDefinitionNode operation)
        {
            var index = new SourceFieldIndex();
            var fragments = document.GetFragments();

            if (schema.TryGetOperationType(operation.Operation, out var rootType))
            {
                index.IndexSelectionSet(
                    schema,
                    fragments,
                    operation.SelectionSet,
                    rootType,
                    string.Empty,
                    []);
            }

            return index;
        }

        public FieldNode? Find(
            FieldNode field,
            string declaringTypeName,
            IList<ISyntaxNode> path)
        {
            var responsePath = CreateResponsePath(path, field);
            var key = new FieldKey(responsePath, declaringTypeName, field.Name.Value);

            if (_fields.TryGetValue(key, out var sourceField))
            {
                return sourceField;
            }

            _fallbackFields.TryGetValue(
                new FallbackFieldKey(responsePath, field.Name.Value),
                out sourceField);
            return sourceField;
        }

        private void IndexSelectionSet(
            Schema schema,
            IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
            SelectionSetNode selectionSet,
            IComplexTypeDefinition declaringType,
            string parentPath,
            HashSet<string> activeFragments)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        var responsePath = AppendResponseName(parentPath, field);
                        _fields.TryAdd(
                            new FieldKey(responsePath, declaringType.Name, field.Name.Value),
                            field);
                        _fallbackFields.TryAdd(
                            new FallbackFieldKey(responsePath, field.Name.Value),
                            field);

                        if (field.SelectionSet is not null
                            && declaringType.Fields.TryGetField(field.Name.Value, out var fieldDefinition)
                            && fieldDefinition.Type.NamedType() is IComplexTypeDefinition childType)
                        {
                            IndexSelectionSet(
                                schema,
                                fragments,
                                field.SelectionSet,
                                childType,
                                responsePath,
                                activeFragments);
                        }
                        break;

                    case InlineFragmentNode inlineFragment:
                        var inlineType = ResolveType(schema, inlineFragment.TypeCondition, declaringType);
                        IndexSelectionSet(
                            schema,
                            fragments,
                            inlineFragment.SelectionSet,
                            inlineType,
                            parentPath,
                            activeFragments);
                        break;

                    case FragmentSpreadNode spread
                        when fragments.TryGetValue(spread.Name.Value, out var fragment)
                            && activeFragments.Add(fragment.Name.Value):
                        var fragmentType = ResolveType(schema, fragment.TypeCondition, declaringType);
                        IndexSelectionSet(
                            schema,
                            fragments,
                            fragment.SelectionSet,
                            fragmentType,
                            parentPath,
                            activeFragments);
                        activeFragments.Remove(fragment.Name.Value);
                        break;
                }
            }
        }

        private static IComplexTypeDefinition ResolveType(
            Schema schema,
            NamedTypeNode? typeCondition,
            IComplexTypeDefinition fallback)
        {
            if (typeCondition is not null
                && schema.Types.TryGetType<IComplexTypeDefinition>(typeCondition.Name.Value, out var type))
            {
                return type;
            }

            return fallback;
        }

        private static string CreateResponsePath(IList<ISyntaxNode> path, FieldNode field)
        {
            var responsePath = string.Empty;

            foreach (var node in path)
            {
                if (node is FieldNode parentField)
                {
                    responsePath = AppendResponseName(responsePath, parentField);
                }
            }

            return AppendResponseName(responsePath, field);
        }

        private static string AppendResponseName(string path, FieldNode field)
        {
            var responseName = field.Alias?.Value ?? field.Name.Value;
            return path.Length == 0 ? responseName : $"{path}.{responseName}";
        }

        private readonly record struct FieldKey(
            string ResponsePath,
            string DeclaringTypeName,
            string FieldName);

        private readonly record struct FallbackFieldKey(string ResponsePath, string FieldName);
    }
}
