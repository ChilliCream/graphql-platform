using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortFieldDefinition
        : InputFieldDefinition
        , IHasScope
    {
        public MemberInfo? Member { get; set; }

        public ISortFieldHandler? Handler { get; set; }

        public string? Scope { get; set; }
    }
}
