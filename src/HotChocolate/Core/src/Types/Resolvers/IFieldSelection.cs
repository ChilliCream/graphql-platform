using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IFieldSelection
    {
        string ResponseName { get; }

        ObjectField Field { get; }

        FieldNode Selection { get; }

        IReadOnlyList<FieldNode> Nodes { get; }
    }
}
