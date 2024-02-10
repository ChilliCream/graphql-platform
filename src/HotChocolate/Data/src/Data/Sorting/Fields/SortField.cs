using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public class SortField
    : InputField
    , ISortField
{
    internal SortField(SortFieldDefinition definition, int index)
        : base(definition, index)
    {
        Member = definition.Member;
        Handler = definition.Handler ??
            throw ThrowHelper.SortField_ArgumentInvalid_NoHandlerWasFound();
        Metadata = definition.Metadata;
    }

    public new SortInputType DeclaringType => (SortInputType)base.DeclaringType;

    SortInputType ISortField.DeclaringType => DeclaringType;

    public MemberInfo? Member { get; }

    public new IExtendedType? RuntimeType { get; private set; }

    public ISortFieldHandler Handler { get; }

    public ISortMetadata? Metadata { get; }

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
