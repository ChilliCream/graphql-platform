using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Stitching.Execution;

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

    public Path Path { get; set; } = Path.Root;

    public NameString Source { get; set; }

    public List<ObjectType> Types { get; } = new();

    public List<ISyntaxNode?> Syntax { get; } = new();

    public Dictionary<ISelection, NameString> Required { get; } = new();

    public Dictionary<Path, NameString> Variables { get; } = new();

    public ObjectPool<List<ISelection>> SelectionList { get; } = new SelectionListObjectPool();
}
