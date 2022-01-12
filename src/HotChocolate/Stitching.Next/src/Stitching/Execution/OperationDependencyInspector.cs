using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

/// <summary>
/// Analyzes operations to determin dependencies between the various remote queries.
/// </summary>
internal sealed class OperationDependencyInspector
{
    private readonly StitchingMetadataDb _metadataDb;

    public OperationDependencyInspector(StitchingMetadataDb metadataDb)
    {
        _metadataDb = metadataDb ?? throw new ArgumentNullException(nameof(metadataDb));
    }

    public void Inspect(IPreparedOperation operation, RemoteQueryPlanerContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ISelectionSet rootSelectionSet = operation.GetRootSelectionSet();
        NameString source = _metadataDb.GetSource(rootSelectionSet.Selections);
        var plan = new QueryNode(source);

        context.Initialize(operation, plan);
        context.Source = _metadataDb.GetSource(rootSelectionSet.Selections);
        context.Types.Push(context.Operation.RootType);
        context.SelectionSets.Push(rootSelectionSet);

        Visit(rootSelectionSet, context);

        context.SelectionSets.Pop();
        context.Types.Pop();
        context.Path = Path.Root;
    }

    private void Visit(ISelectionSet selectionSet, RemoteQueryPlanerContext context)
    {
        var source = context.Source;
        var declaringType = context.Types.Peek();
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
        }

        context.Source = source;
        context.SelectionList.Return(selections);
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

                Visit(selectionSet, context);

                context.SelectionSets.Pop();
                context.Types.Pop();
            }
        }

        context.Path = parentPath;
    }
}
