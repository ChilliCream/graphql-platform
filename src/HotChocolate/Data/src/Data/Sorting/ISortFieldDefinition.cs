using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public interface ISortFieldDefinition
    : IDefinition
    , IHasDirectiveDefinition
    , IHasIgnore
    , IHasScope
{
    public MemberInfo? Member { get; }

    public ISortFieldHandler? Handler { get; }

    public Expression? Expression { get; }
}
