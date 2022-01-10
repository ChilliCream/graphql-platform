using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Stitching.Execution;

internal sealed class RemoteQueryPlaner
{
    private StitchingMetadataDb _metadataDb;

    public RemoteQueryPlaner(StitchingMetadataDb metadataDb)
    {
        _metadataDb = metadataDb ?? throw new ArgumentNullException(nameof(metadataDb));
    }

    public QueryNode Build(IPreparedOperation operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        return Visit(operation);
    }

    private QueryNode Visit(IPreparedOperation operation)
    {
        ISelectionSet rootSelectionSet = operation.GetRootSelectionSet();
        ISelection first = rootSelectionSet.Selections[0];
        NameString source = _metadataDb.GetSource(first);
        QueryNode root = new QueryNode(source);
        RemoteQueryPlanerContext context = new RemoteQueryPlanerContext(operation, root);
        context.Syntax.Push(default);

        Visit(rootSelectionSet, context);

        ISyntaxNode? node = context.Syntax.Pop();

        if (node is not SelectionSetNode selectionSetSyntax)
        {
            throw new InvalidOperationException();
        }

        var operationSyntax = new OperationDefinitionNode(
            null,
            null,
            operation.Definition.Operation,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSetSyntax);

        root.Document = new DocumentNode(null, new[] { operationSyntax });

        return root;
    }

    private void Visit(ISelectionVariants selectionVariants, RemoteQueryPlanerContext context)
    {

    }

    private void Visit(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {
        Enter(selectionSet, context);

        var currentNode = context.CurrentNode;
        var set = new HashSet<ISelection>();

        foreach (ISelection selection in selectionSet.Selections)
        {
            if (_metadataDb.IsPartOfSource(currentNode.Source, selection))
            {

            }
        }

        while (buffered.Count > 0)
        {
            while (buffered.Count < index)
            {

            }

            if (buffered.Count > 0)
            {

            }
        }

        foreach (ISelection selection in selectionSet.Selections)
        {
            if (metadataDb.IsPartOfSource(currentNode.Source, selection))
            {
                Visit(selection, context);
            }
            else
            {

            }
        }

        Leave(selectionSet, context);
    }

    private void Enter(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {

    }

    private void Leave(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {

    }

    private void Visit(ISelection selection, RemoteQueryPlanerContext context)
    {
        Enter(selection, context);

        if (selection.SelectionSet is not null)
        {
            foreach (ObjectType possibleType in
                context.Operation.GetPossibleTypes(selection.SelectionSet))
            {
                ISelectionSet selectionSet =
                    context.Operation.GetSelectionSet(selection.SelectionSet, possibleType);
                Visit(selectionSet, context);
            }
        }

        Leave(selection, context);
    }

    private void Enter(ISelection selection, RemoteQueryPlanerContext context)
    {

    }

    private void Leave(ISelection selection, RemoteQueryPlanerContext context)
    {

    }
}

internal sealed class OperationInspector
{
    private StitchingMetadataDb _metadataDb;

    public OperationInspector(StitchingMetadataDb metadataDb)
    {
        _metadataDb = metadataDb ?? throw new ArgumentNullException(nameof(metadataDb));
    }

    public QueryNode Inspect(RemoteQueryPlanerContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return Visit(operation);
    }

    private void Visit(ISelectionVariants selectionVariants, RemoteQueryPlanerContext context)
    {

    }

    private void Visit(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {
        var source = context.Source;
        var selections = context.SelectionList.Get();
        selections.AddRange(selectionSet.Selections);

        while (selections.Count > 0)
        {
            int index = 0;
            while (index < selections.Count)
            {
                ISelection selection = selections[index];
                if (_metadataDb.IsPartOfSource(context.Source, selection))
                {
                    Visit(selection, context);
                    selections.Remove(selection);
                }
                else
                {
                    index++;
                }
            }

            if (selections.Count > 0)
            {
                context.Source = _metadataDb.GetSource(selections);
                
                var fetcher = _metadataDb.GetObjectFetcher(
                    context.Source,
                    selections[0].DeclaringType,
                    context.Types);
                
                foreach (ArgumentInfo argument in fetcher.Arguments)
                {
                    argument.Binding,
                }
            }
        }

        context.Source = source;
        context.SelectionList.Return(selections);
    }

    private void Visit(ISelection selection, RemoteQueryPlanerContext context)
    {
        if (selection.SelectionSet is not null)
        {
            foreach (ObjectType possibleType in
                context.Operation.GetPossibleTypes(selection.SelectionSet))
            {
                ISelectionSet selectionSet =
                    context.Operation.GetSelectionSet(selection.SelectionSet, possibleType);
                Visit(selectionSet, context);
            }
        }
    }
}

internal sealed class RemoteQueryPlanerContext
{
    public RemoteQueryPlanerContext(IPreparedOperation operation, QueryNode root)
    {
        Operation = operation;
        Plan = root;
        CurrentNode = root;
    }

    public IPreparedOperation Operation { get; }

    public QueryNode Plan { get; }

    public QueryNode CurrentNode { get; set; }

    public NameString Source { get; set; }

    public List<ObjectType> Types { get; } = new();

    public List<ISyntaxNode?> Syntax { get; } = new();

    public Dictionary<ISelection, NameString> Required { get; } = new();

    public Dictionary<Path, NameString> Variables { get; } = new();

    public ObjectPool<List<ISelection>> SelectionList { get; } = new SelectionListObjectPool();
}

internal sealed class SelectionListObjectPool : ObjectPool<List<ISelection>>
{
    private const int _maxSize = 16;
    private List<List<ISelection>> _pool = new();

    public override List<ISelection> Get()
    {
        return _pool.TryPop(out var selection)
            ? selection
            : new List<ISelection>();
    }

    public override void Return(List<ISelection> obj)
    {
        obj.Clear();

        if (_pool.Count < _maxSize)
        {
            _pool.Push(obj);
        }
    }
}
