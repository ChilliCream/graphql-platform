using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Data.ErrorHelper;


namespace HotChocolate.Data.Filters
{
    public class FilterField
        : InputField
        , IFilterField
    {
        private Type _runtimeType = default!;

        internal FilterField(FilterFieldDefinition definition)
            : base(definition)
        {
            Member = definition.Member;
            Handler = definition.Handler;
        }

        public override Type RuntimeType => _runtimeType;

        public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

        IFilterInputType IFilterField.DeclaringType => DeclaringType;

        public MemberInfo? Member { get; }

        public Type? ElementType { get; }

        public FilterFieldHandler? Handler { get; }

        public bool? IsNullable => TypeInfo?.IsNullable;

        public FilterTypeInfo? TypeInfo { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            if (Member is { } && Member.DeclaringType != null)
            {
                IExtendedType extendedTypeInfo;
                if (Member is PropertyInfo propertyInfo)
                {
                    extendedTypeInfo = new NullableHelper(Member.DeclaringType)
                        .GetPropertyInfo(propertyInfo);
                    _runtimeType = propertyInfo.PropertyType;
                }
                else if (Member is MethodInfo methodInfo)
                {
                    extendedTypeInfo = new NullableHelper(Member.DeclaringType)
                        .GetMethodInfo(methodInfo).ReturnType;
                    _runtimeType = methodInfo.ReturnType;
                }
                else
                {
                    context.ReportError(FilterField_RuntimeType_Unknown(this));
                }

                TypeInfo = FilterTypeInfo.From(extendedTypeInfo);

                if (DotNetTypeInfoFactory.IsListType(_runtimeType))
                {
                    if (!TypeInspector.Default.TryCreate(
                        _runtimeType,
                        out Utilities.TypeInfo typeInfo))
                    {
                        throw new ArgumentException(
                            string.Format(
                                InvariantCulture,
                                FilterField_FilterField_TypeUnknown,
                                _runtimeType.Name),
                            nameof(definition));
                    }
                    ElementType = typeInfo.ClrType;
                }
            }

        }
    }
}
