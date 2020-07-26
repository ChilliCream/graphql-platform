using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Utilities;

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

            if (Member is { } &&
                Member.DeclaringType != null)
            {
                if (Member is PropertyInfo propertyInfo)
                {
                    IsNullable = new NullableHelper(
                        Member.DeclaringType).GetPropertyInfo(propertyInfo).IsNullable;
                }
                else if (Member is MethodInfo methodInfo)
                {
                    IsNullable = new NullableHelper(
                        Member.DeclaringType).GetMethodInfo(methodInfo).ReturnType.IsNullable;
                }
            }
        }

        public MemberInfo? Member { get; }

        public FilterFieldHandler? Handler { get; }

        public bool IsNullable { get; }
    }
}
