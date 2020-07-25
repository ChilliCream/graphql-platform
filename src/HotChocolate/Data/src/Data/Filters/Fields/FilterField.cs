using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class FilterField
        : InputField
        , IFilterField
    {
        internal FilterField(FilterFieldDefinition definition)
            : base(definition)
        {
            Member = definition.Member;
            Handler = definition.Handler;
        }

        public MemberInfo? Member { get; set; }

        public FilterFieldHandler? Handler { get; set; }
    }
}
