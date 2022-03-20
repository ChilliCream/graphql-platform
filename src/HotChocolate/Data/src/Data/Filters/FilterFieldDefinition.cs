using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterFieldDefinition
    : InputFieldDefinition
    , IHasScope
    , IFilterFieldDefinition
{
    private List<int>? _allowedOperations;

    public MemberInfo? Member { get; set; }

    public IFilterFieldHandler? Handler { get; set; }

    public string? Scope { get; set; }

    public List<int> AllowedOperations => _allowedOperations ??= new List<int>();

    public bool HasAllowedOperations => _allowedOperations?.Count > 0;
}
