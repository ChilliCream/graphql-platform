using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

// May want something like this.
public readonly struct ExpressionNodeCreationSource
{
    private static readonly List<string> _description = new();

    public static ExpressionNodeCreationSource New(string description)
    {
        lock (_description)
        {
            var id = _description.Count;
            _description.Add(description);
            return new(id);
        }
    }

    private readonly int _id;

    private ExpressionNodeCreationSource(int id)
    {
        _id = id;
    }

    // ReSharper disable once InconsistentlySynchronizedField
    public override string ToString() => _description[_id];
}
