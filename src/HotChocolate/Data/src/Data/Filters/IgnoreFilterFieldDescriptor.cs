using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    internal class IgnoreFilterFieldDescriptor
        : FilterFieldDescriptor
    {
        protected IgnoreFilterFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            MemberInfo member)
            : base(context, scope)
        {
            Definition.Member = member;
            Definition.Ignore = true;
        }

        public static FilterFieldDescriptor New(
            IDescriptorContext context,
            string? scope,
            MemberInfo member) =>
            new IgnoreFilterFieldDescriptor(context, scope, member);
    }
}
