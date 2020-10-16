using System.Reflection;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldDefinition
    {
        MemberInfo? Member { get; set; }

        IFilterFieldHandler? Handler { get; set; }

        string? Scope { get; set; }
    }
}
