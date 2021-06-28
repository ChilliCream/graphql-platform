using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    public class IgnoreSortFieldDescriptor
        : SortFieldDescriptor
    {
        protected IgnoreSortFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            MemberInfo member)
            : base(context, scope)
        {
            Definition.Member = member;
            Definition.Ignore = true;
        }

        public static IgnoreSortFieldDescriptor New(
            IDescriptorContext context,
            string? scope,
            MemberInfo member) =>
            new IgnoreSortFieldDescriptor(context, scope, member);
    }
}
