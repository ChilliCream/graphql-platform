using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

internal sealed class OperationInspector
{
    private readonly StitchingMetadataDb _metadataDb;

    public OperationInspector(StitchingMetadataDb metadataDb)
    {
        _metadataDb = metadataDb ?? throw new ArgumentNullException(nameof(metadataDb));
    }

    public void Inspect(RemoteQueryPlanerContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ISelectionSet rootSelectionSet = context.Operation.GetRootSelectionSet();
        Visit(rootSelectionSet, context);
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
            foreach (IObjectType possibleType in
                context.Operation.GetPossibleTypes(selection.SelectionSet))
            {
                ISelectionSet selectionSet =
                    context.Operation.GetSelectionSet(selection.SelectionSet, possibleType);
                Visit(selectionSet, context);
            }
        }
    }
}
