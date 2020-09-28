using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortField
        : InputField,
          ISortField
    {
        internal SortField(SortFieldDefinition definition)
            : base(definition)
        {
            Member = definition.Member;
            Handler = definition.Handler ??
                throw new InvalidOperationException();
        }

        public new SortInputType DeclaringType => (SortInputType)base.DeclaringType;

        SortInputType ISortField.DeclaringType => DeclaringType;

        public MemberInfo? Member { get; }

        public new IExtendedType? RuntimeType { get; private set; }

        public ISortFieldHandler Handler { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            if (Member?.DeclaringType is not null)
            {
                RuntimeType = context.TypeInspector.GetReturnType(Member);
            }
        }
    }
}
