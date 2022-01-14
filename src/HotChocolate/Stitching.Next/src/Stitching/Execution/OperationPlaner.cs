using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

internal sealed class OperationPlaner
{
    private StitchingMetadataDb _metadataDb;

    public OperationPlaner(StitchingMetadataDb metadataDb)
    {
        _metadataDb = metadataDb ?? throw new ArgumentNullException(nameof(metadataDb));
    }

    public QueryNode Build(IPreparedOperation operation, OperationPlanerContext context)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        ISelectionSet rootSelectionSet = operation.GetRootSelectionSet();
        NameString source = _metadataDb.GetSource(rootSelectionSet.Selections);
        var plan = new QueryNode(source);

        context.Initialize(operation, plan);
        context.Source = _metadataDb.GetSource(rootSelectionSet.Selections);
        context.Types.Push(context.Operation.RootType);
        context.SelectionSets.Push(rootSelectionSet);
        context.Syntax.Push(null);

        Visit(rootSelectionSet, context);

        context.Plan.Document = CreateDocument(context);
        context.SelectionSets.Pop();
        context.Types.Pop();
        context.Path = Path.Root;

        return context.Plan;
    }

    private void Visit(ISelectionSet selectionSet, OperationPlanerContext context)
    {
        var selectionSyntax = new List<ISelectionNode>();
        var exports = new List<NameString>();
        var variables = default(List<VariableDefinitionNode>);
        var fetcher = default(ObjectFetcherInfo?);
        var needsOperation = false;

        var node = context.CurrentNode;
        var source = context.Source;
        var declaringType = context.Types.Peek();
        var selections = context.SelectionList.Get();
        var needsInlineFragment = context.NeedsInlineFragment;

        selections.AddRange(selectionSet.Selections);

        // determine if we need any exports for the current selectionSet.
        if (context.RequiredFields.TryGetValue(selectionSet, out var fields))
        {
            exports.AddRange(fields);
        }

        while (selections.Count > 0 || exports.Count > 0)
        {
            // first we take all the selections and exports 
            // that can be resolved with the current source.
            ExtractSelectionsForSource(
                context.Source,
                selections,
                selectionSyntax,
                context);

            ExtractExportsForSource(
                context.Source,
                declaringType,
                exports,
                selectionSyntax,
                context);

            // after resolving all selections and exports that can be requested
            // from the current source we will build the operation syntax.
            BuildOperation();

            // next we will check if there are still selections or exports to 
            // resolve. If there are more selections or exports to resolve we will
            // determine the source with the highest score.
            ResolveNextSource();
        }

        context.Source = source;
        context.CurrentNode = node;
        context.SelectionList.Return(selections);

        void ResolveNextSource()
        {
            if (selections.Count > 0)
            {
                context.Source = _metadataDb.GetSource(selections);

                fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);

                PrepareOperation(fetcher.Value);
            }
            else if (exports.Count > 0)
            {
                context.Source = _metadataDb.GetSource(context.Types.Peek(), exports);

                fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);

                PrepareOperation(fetcher.Value);
            }
        }

        void PrepareOperation(ObjectFetcherInfo fetcher)
        {
            needsOperation = true;

            if (variables is null || variables.Count > 0)
            {
                variables = new List<VariableDefinitionNode>();
            }

            selectionSyntax = new List<ISelectionNode>();

            var newNode = new QueryNode(context.Source);
            node.Nodes.Add(newNode);
            context.CurrentNode = newNode;

            foreach (ArgumentInfo argument in fetcher.Arguments)
            {
                string typeName = argument.Binding.Name;
                string memberName = argument.Binding.MemberName!;
                string variableName = $"_export_{typeName}_{memberName}";

                variables.Add(
                    new VariableDefinitionNode(
                        null,
                        new(variableName),
                        argument.Type,
                        null,
                        Array.Empty<DirectiveNode>()));
            }
        }

        void BuildOperation()
        {
            var selectionSetSyntax = new SelectionSetNode(selectionSyntax);

            if (needsInlineFragment)
            {
                selectionSyntax.Insert(0, TypeNameField);

                var inlineFragment = new InlineFragmentNode(
                    null,
                    new NamedTypeNode(context.Types.Peek().Name),
                    Array.Empty<DirectiveNode>(),
                    selectionSetSyntax);

                selectionSetSyntax = new SelectionSetNode(new[] { inlineFragment });
                needsInlineFragment = false;
            }

            if (needsOperation)
            {
                if (fetcher is not null && fetcher.Value.Selections is FieldNode field)
                {
                    field = field.WithSelectionSet(selectionSetSyntax);

                    if (fetcher.Value.Arguments.Count > 0 && variables is not null)
                    {
                        var arguments = new List<ArgumentNode>();
                        for (int i = 0; i < fetcher.Value.Arguments.Count; i++)
                        {
                            ArgumentInfo argument = fetcher.Value.Arguments[i];
                            VariableDefinitionNode variable = variables[i];
                            arguments.Add(new ArgumentNode(argument.Name, variable.Variable));
                        }
                        field = field.WithArguments(arguments);
                    }

                    selectionSetSyntax = new SelectionSetNode(new[] { field });
                }

                context.Syntax.Push(selectionSetSyntax);
                context.CurrentNode.Document = CreateDocument(context, variables);
            }
            else
            {
                context.Syntax.Set(selectionSetSyntax);
            }
        }
    }

    private void Visit(ISelection selection, OperationPlanerContext context)
    {
        Path parentPath = context.Path;
        context.Path = context.Path.Append(selection.ResponseName);

        if (selection.SelectionSet is not null)
        {
            SelectionSetNode selectionSetNode = selection.SelectionSet;
            IPreparedOperation operation = context.Operation;
            IReadOnlyList<IObjectType> types = operation.GetPossibleTypes(selectionSetNode);
            context.NeedsInlineFragment = types.Count > 1;

            foreach (IObjectType type in types)
            {
                ISelectionSet selectionSet = operation.GetSelectionSet(selectionSetNode, type);

                context.Types.Push(type);
                context.SelectionSets.Push(selectionSet);
                context.Syntax.Push(null);

                Visit(selectionSet, context);

                context.Syntax.Set(
                    selection.SyntaxNode.WithSelectionSet(
                        (SelectionSetNode)context.Syntax.Pop()!));
                context.SelectionSets.Pop();
                context.Types.Pop();
            }
        }
        else
        {
            context.Syntax.Set(selection.SyntaxNode);
        }

        context.Path = parentPath;
    }

    private void ExtractSelectionsForSource(
        NameString source,
        List<ISelection> selections,
        List<ISelectionNode> selectionSyntax,
        OperationPlanerContext context)
    {
        int index = 0;
        while (index < selections.Count)
        {
            ISelection selection = selections[index];
            if (_metadataDb.IsPartOfSource(source, selection))
            {
                context.Syntax.Push(null);
                Visit(selection, context);
                selectionSyntax.Add((ISelectionNode)context.Syntax.Pop()!);
                selections.Remove(selection);
            }
            else
            {
                index++;
            }
        }
    }

    private void ExtractExportsForSource(
        NameString source,
        IObjectType declaringType,
        List<NameString> exports,
        List<ISelectionNode> selectionSyntax,
        OperationPlanerContext context)
    {
        int index = 0;
        while (index < exports.Count)
        {
            NameString fieldName = exports[index];
            if (_metadataDb.IsPartOfSource(source, declaringType, fieldName))
            {
                selectionSyntax.Add(new FieldNode(
                    null,
                    new NameNode(fieldName),
                    new NameNode($"_export_{declaringType.Name}_{fieldName}"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));
                exports.Remove(fieldName);
            }
            else
            {
                index++;
            }
        }
    }

    private DocumentNode CreateDocument(
        OperationPlanerContext context,
        IReadOnlyList<VariableDefinitionNode>? variables = null)
    {
        context.Segments++;

        ISyntaxNode? node = context.Syntax.Pop();

        if (node is not SelectionSetNode selectionSetSyntax)
        {
            throw new InvalidOperationException();
        }

        var operationSyntax = new OperationDefinitionNode(
            null,
            CreateOperationName(),
            context.Operation.Definition.Operation,
            variables ?? Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSetSyntax);

        return new DocumentNode(null, new[] { operationSyntax });

        NameNode CreateOperationName()
        {
            if (context.Operation.Definition.Name is null)
            {
                string safeId = BitConverter.ToString(
                    Encoding.UTF8.GetBytes(context.Operation.Id))
                    .Replace("-", string.Empty);
                return new NameNode($"Operation_{safeId}_{context.Segments}");
            }

            return new NameNode($"{context.Operation.Definition.Name.Value}_{context.Segments}");
        }
    }

    private static string CreateExportName(SchemaCoordinate binding)
        => $"_export_{binding.Name}_{binding.MemberName}";

    private static FieldNode TypeNameField { get; } =
        new FieldNode(
            null, 
            new NameNode("__typename"), 
            null, 
            null, 
            Array.Empty<DirectiveNode>(), 
            Array.Empty<ArgumentNode>(), 
            null);
}
