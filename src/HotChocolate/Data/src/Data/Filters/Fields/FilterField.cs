using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterField
    : InputField
    , IFilterField
{
    internal FilterField(FilterFieldDefinition definition)
        : this(definition, default)
    {
    }

    internal FilterField(FilterFieldDefinition definition, int index)
        : base(definition, index)
    {
        Member = definition.Member;
        Handler = definition.Handler!;
        Metadata = definition.Metadata;
    }

    public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

    IFilterInputType IFilterField.DeclaringType => DeclaringType;

    public MemberInfo? Member { get; }

    public new IExtendedType? RuntimeType { get; private set; }

    public IFilterFieldHandler Handler { get; }

    public IFilterMetadata? Metadata { get; }

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldDefinition definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        if (Member?.DeclaringType is not null)
        {
            RuntimeType = context.TypeInspector.GetReturnType(Member, ignoreAttributes: true);
        }
        else if (base.RuntimeType is { } runtimeType)
        {
            RuntimeType = context.TypeInspector.GetType(runtimeType);
        }
    }
}
