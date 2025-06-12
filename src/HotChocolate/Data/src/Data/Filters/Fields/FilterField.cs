using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public class FilterField
    : InputField
    , IFilterField
{
    internal FilterField(FilterFieldConfiguration configuration)
        : this(configuration, 0)
    {
    }

    internal FilterField(FilterFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
        Member = configuration.Member;
        Handler = configuration.Handler!;
        Metadata = configuration.Metadata;
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
        InputFieldConfiguration definition)
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
