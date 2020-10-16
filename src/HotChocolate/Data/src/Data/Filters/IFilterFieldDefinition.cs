using System.Reflection;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldDefinition
    {
        MemberInfo? Member { get; }

        IFilterFieldHandler? Handler { get; }

        string? Scope { get; }
    }
}
