using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
    {
    public class FilterFieldDefinition : InputFieldDefinition
    {
        public MemberInfo? Member { get; set; }
    }
}
