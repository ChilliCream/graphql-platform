using System;
using System.Collections.Generic;
using System.Linq;
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

    public QueryNode Build(IPreparedOperation operation, RemoteQueryPlanerContext context)
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

        context.SelectionSets.Pop();
        context.Types.Pop();
        context.Path = Path.Root;
        return context.Plan;
    }

    private void Visit(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {
        var source = context.Source;
        var declaringType = context.Types.Peek();
        var selectionsToProcess = context.SelectionList.Get();
        var selections = new List<ISelectionNode>();
        var export = new List<NameString>();

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

            if (selectionsToProcess.Count > 0)
            {
                context.Source = _metadataDb.GetSource(selectionsToProcess);

                var fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);

                foreach (ArgumentInfo argument in fetcher.Arguments)
                {
                    if (argument.Binding.Name.Equals(declaringType.Name))
                    {
                        context.RegisterRequiredField(
                            selectionSet,
                            argument.Binding.MemberName!.Value);
                    }
                    else
                    {
                        for (var i = context.Types.Count - 2; i >= 0; i--)
                        {
                            if (argument.Binding.Name.Equals(context.Types[i].Name))
                            {
                                context.RegisterRequiredField(
                                    context.SelectionSets[i],
                                    argument.Binding.MemberName!.Value);
                                break;
                            }
                        }
                    }
                }
            }
            else if (export.Count > 0)
            {
                context.Source = _metadataDb.GetSource(context.Types.Peek(), export);

                var fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    declaringType,
                    context.Types);
            }
        }

        context.Source = source;
        context.SelectionList.Return(selectionsToProcess);
    }

    private void Visit(ISelection selection, RemoteQueryPlanerContext context)
    {
        Path parentPath = context.Path;
        context.Path = context.Path.Append(selection.ResponseName);

        if (selection.SelectionSet is not null)
        {
            SelectionSetNode selectionSetNode = selection.SelectionSet;
            IPreparedOperation operation = context.Operation;

            foreach (IObjectType type in operation.GetPossibleTypes(selectionSetNode))
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
        RemoteQueryPlanerContext context)
    {
        ISyntaxNode? node = context.Syntax.Pop();

        if (node is not SelectionSetNode selectionSetSyntax)
        {
            throw new InvalidOperationException();
        }

        var operationSyntax = new OperationDefinitionNode(
            null,
            null,
            context.Operation.Definition.Operation,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSetSyntax);

        return new DocumentNode(null, new[] { operationSyntax });
    }
}
