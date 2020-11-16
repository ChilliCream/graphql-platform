using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldDefinition
        : InputFieldDefinition
        , IHasScope
        , IFilterFieldDefinition
    {
        public MemberInfo? Member { get; set; }

        public IFilterFieldHandler? Handler { get; set; }

        public string? Scope { get; set; }
    }
}
