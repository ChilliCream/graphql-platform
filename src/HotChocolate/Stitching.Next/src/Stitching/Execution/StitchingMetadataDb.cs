using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

internal sealed class StitchingMetadataDb
{
    public bool IsPartOfSource(NameString source, ISelection selection)
    {
        return true;
    }

    public NameString GetSource(ISelection selection)
    {
        return Schema.DefaultName;
    }

    public NameString GetSource(IReadOnlyCollection<ISelection> selections)
    {
        return Schema.DefaultName;
    }

    public ObjectFetcherInfo GetObjectFetcher(
        NameString source,
        IObjectType type,
        IReadOnlyList<ObjectType> typesInPath)
    {
        return default!;
    }

}
