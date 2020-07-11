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
        }

        public MemberInfo? Member { get; set; }
    }
}
