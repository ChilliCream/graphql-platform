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
        var source = context.Source;
        var node = context.CurrentNode;
        var declaringType = context.Types.Peek();
        var selectionsToProcess = context.SelectionList.Get();
        var selections = new List<ISelectionNode>();
        var export = new List<NameString>();
        List<VariableDefinitionNode>? variables = null;
        var needsInlineFragment = context.NeedsInlineFragment;
        ObjectFetcherInfo? fetcher = null;
        var needsOperation = false;

        selectionsToProcess.AddRange(selectionSet.Selections);

        if (context.RequiredFields.TryGetValue(selectionSet, out var fields))
        {
            export.AddRange(fields);
        }

        while (selectionsToProcess.Count > 0 || export.Count > 0)
        {
            int index = 0;
            while (index < selectionsToProcess.Count)
            {
                ISelection selection = selectionsToProcess[index];
                if (_metadataDb.IsPartOfSource(context.Source, selection))
                {
                    context.Syntax.Push(null);
                    Visit(selection, context);
                    selections.Add((ISelectionNode)context.Syntax.Pop()!);
                    selectionsToProcess.Remove(selection);
                }
                else
                {
                    index++;
                }
            }

            index = 0;
            while (index < export.Count)
            {
                NameString fieldName = export[index];
                if (_metadataDb.IsPartOfSource(context.Source, context.Types.Peek(), fieldName))
                {
                    selections.Add(new FieldNode(
                        null,
                        new NameNode(fieldName),
                        new NameNode($"_export_{context.Types.Peek().Name}_{fieldName}"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null));
                    export.Remove(fieldName);
                }
                else
                {
                    index++;
                }
            }

            BuildOperation();

            if (selectionsToProcess.Count > 0)
            {
                context.Source = _metadataDb.GetSource(selectionsToProcess);

                fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);

                PrepareOperation(fetcher.Value);
            }
            else if (export.Count > 0)
            {
                context.Source = _metadataDb.GetSource(context.Types.Peek(), export);

                fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);

                PrepareOperation(fetcher.Value);
            }
        }

        context.Source = source;
        context.CurrentNode = node;
        context.SelectionList.Return(selectionsToProcess);

        void PrepareOperation(ObjectFetcherInfo fetcher)
        {
            needsOperation = true;

            if (variables is null || variables.Count > 0)
            {
                variables = new List<VariableDefinitionNode>();
            }

            selections = new List<ISelectionNode>();

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
            var selectionSetSyntax = new SelectionSetNode(selections);

            if (needsInlineFragment)
            {
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
}
