using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public class SortFieldDefinition
    : InputFieldDefinition
    , ISortFieldDefinition
{
    public MemberInfo? Member { get; set; }

    public ISortFieldHandler? Handler { get; set; }

    public string? Scope { get; set; }

    public Expression? Expression { get; set; }

    internal ISortMetadata? Metadata { get; set; }

    internal void CopyTo(SortFieldDefinition target)
    {
        base.CopyTo(target);

        target.Member = Member;
        target.Handler = Handler;
        target.Expression = Expression;
        target.Metadata = Metadata;
        target.Scope = Scope;
    }

    internal void MergeInto(SortFieldDefinition target)
    {
        base.MergeInto(target);

        if (Member is not null)
        {
            target.Member = Member;
        }

        if (Handler is not null)
        {
            target.Handler = Handler;
        }

        if (Scope is not null)
        {
            target.Scope = Scope;
        }

        if (Expression is not null)
        {
            target.Expression = Expression;
        }

        if (Metadata is not null)
        {
            target.Metadata = Metadata;
        }
    }
}
