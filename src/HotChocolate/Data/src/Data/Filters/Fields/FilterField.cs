using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterField
        : InputField
        , IFilterField
    {
        private Type runtimeType;

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

                    runtimeType = propertyInfo.PropertyType;
                }
                else if (Member is MethodInfo methodInfo)
                {
                    IsNullable = new NullableHelper(
                        Member.DeclaringType).GetMethodInfo(methodInfo).ReturnType.IsNullable;

                    runtimeType = methodInfo.ReturnType;
                }
                if (DotNetTypeInfoFactory.IsListType(runtimeType))
                {
                    if (!TypeInspector.Default.TryCreate(
                        runtimeType,
                        out Utilities.TypeInfo typeInfo))
                    {
                        throw new ArgumentException(
                            string.Format("The type {0} is unknown", runtimeType.Name),
                            nameof(runtimeType));
                    }
                    ElementType = typeInfo.ClrType;
                }
            }
        }

        public override Type RuntimeType => runtimeType;

        public MemberInfo? Member { get; }

        public Type? ElementType { get; }

        public FilterFieldHandler? Handler { get; }

        public bool IsNullable { get; }
    }
}
