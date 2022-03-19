using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterFieldDefinition
    : InputFieldDefinition
    , IHasScope
    , IFilterFieldDefinition
{
    public MemberInfo? Member { get; set; }

    public IFilterFieldHandler? Handler { get; set; }

    public Expression? Expression { get; set; }

    internal IFilterMetadata? Metadata { get; set; }

    public string? Scope { get; set; }
}
