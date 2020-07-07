using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterMethodDefinition : InputFieldDefinition
    {
        public int FieldKind { get; set; }

        public int MethodKind { get; set; }

        public MemberInfo? Member { get; set; }
    }
}
