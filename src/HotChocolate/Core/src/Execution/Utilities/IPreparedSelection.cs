using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedSelection
    {
        ObjectType DeclaringType { get; }

        ObjectField Field { get; }

        FieldNode Selection { get; }

        IReadOnlyList<FieldNode> Selections { get; }

        int ResponseIndex { get; }

        string ResponseName { get; }

        FieldDelegate ResolverPipeline { get; }

        object Arguments { get; }

        bool IsVisible(IVariableValueCollection variables);
    }
}
