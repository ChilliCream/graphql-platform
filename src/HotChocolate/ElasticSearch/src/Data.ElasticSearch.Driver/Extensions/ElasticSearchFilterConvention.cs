using System.Reflection;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.ElasticSearch;

public class ElasticSearchFilterConvention : FilterConvention
{
    private ITypeInspector _typeInspector = default!;
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddElasticSearchDefaults();
    }

    protected override void Complete(IConventionContext context)
    {
        _typeInspector = context.DescriptorContext.TypeInspector;
        base.Complete(context);
    }

    public override ExtendedTypeReference GetFieldType(MemberInfo member)
    {
        IExtendedType runtimeType = _typeInspector.GetReturnType(member, true);
        if (runtimeType.IsArrayOrList)
        {
            if (runtimeType.ElementType is { } &&
                base.GetFieldType(runtimeType.ElementType.Source) is { } elementType)
            {
                return _typeInspector.GetTypeRef(
                    typeof(ArrayFilterInputType<>).MakeGenericType(elementType.Type.Source),
                    TypeContext.Input,
                    Scope);
            }
        }

        return base.GetFieldType(member);
    }
}
