using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

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

        public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

        IFilterInputType IFilterField.DeclaringType => DeclaringType;

        public MemberInfo? Member { get; }

        public new IExtendedType? RuntimeType { get; private set; }

        public IFilterFieldHandler Handler { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            if (Member?.DeclaringType is not null)
            {
                RuntimeType = context.TypeInspector.GetReturnType(Member, ignoreAttributes: true);
            }
        }
    }
}
