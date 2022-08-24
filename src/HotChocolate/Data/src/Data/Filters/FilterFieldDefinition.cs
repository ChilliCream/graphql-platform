using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterFieldDefinition
    : InputFieldDefinition, IFilterFieldDefinition
{
    private List<int>? _allowedOperations;

    public MemberInfo? Member { get; set; }

    public IFilterFieldHandler? Handler { get; set; }

    public Expression? Expression { get; set; }

    internal IFilterMetadata? Metadata { get; set; }

    public string? Scope { get; set; }

    public List<int> AllowedOperations => _allowedOperations ??= new List<int>();

    public bool HasAllowedOperations => _allowedOperations?.Count > 0;

    public Func<IDescriptorContext, string?, FilterInputTypeDefinition>? CreateFieldTypeDefinition
    {
        get;
        set;
    }

    internal void CopyTo(FilterFieldDefinition target)
    {
        base.CopyTo(target);

        target.Member = Member;
        target._allowedOperations = _allowedOperations;
        target.Handler = Handler;
        target.Scope = Scope;
        target.CreateFieldTypeDefinition = CreateFieldTypeDefinition;
    }

    internal void MergeInto(FilterFieldDefinition target)
    {
        base.MergeInto(target);

        if (Member is not null)
        {
            target.Member = Member;
        }
        if (_allowedOperations is { Count: > 0 })
        {
            target._allowedOperations = _allowedOperations;
        }
        if (Handler is not null)
        {
            target.Handler = Handler;
        }
        if (Scope is not null)
        {
            target.Scope = Scope;
        }
        if (CreateFieldTypeDefinition is not null)
        {
            target.CreateFieldTypeDefinition = CreateFieldTypeDefinition;
        }
    }
}
