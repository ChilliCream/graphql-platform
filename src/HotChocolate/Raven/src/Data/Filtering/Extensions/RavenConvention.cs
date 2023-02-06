using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Raven.Filtering;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data;

internal sealed class RavenConvention : FilterConvention
{
    private ITypeInspector _typeInspector = null!;

    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor
            .AddDefaultOperations()
            .BindDefaultTypes()
            .Provider<RavenQueryableFilterProvider>();
    }

    protected override void Complete(IConventionContext context)
    {
        base.Complete(context);

        _typeInspector = context.DescriptorContext.TypeInspector;
    }

    public override ExtendedTypeReference GetFieldType(MemberInfo member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        var runtimeType = _typeInspector.GetReturnType(member, true);

        if (runtimeType.IsArrayOrList)
        {
            if (runtimeType.ElementType is { } &&
                TryCreateFilterType(runtimeType.ElementType, out var elementType))
            {
                return _typeInspector.GetTypeRef(
                    typeof(RavenListFilterInputType<>).MakeGenericType(elementType),
                    TypeContext.Input,
                    Scope);
            }
        }

        return base.GetFieldType(member);
    }
}
