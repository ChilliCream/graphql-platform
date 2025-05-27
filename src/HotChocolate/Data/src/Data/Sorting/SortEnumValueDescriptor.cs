using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class SortEnumValueDescriptor
    : EnumValueDescriptor,
      ISortEnumValueDescriptor
{
    protected SortEnumValueDescriptor(
        IDescriptorContext context,
        string? scope,
        int value)
        : base(context, new SortEnumValueConfiguration { Operation = value, })
    {
        var convention = context.GetSortConvention(scope);
        Configuration.Name = convention.GetOperationName(value);
        Configuration.Description = convention.GetOperationDescription(value);
        Configuration.RuntimeValue = Configuration.Name;
    }

    protected SortEnumValueDescriptor(
        IDescriptorContext context,
        SortEnumValueConfiguration configuration)
        : base(context, configuration)
    {
    }

    protected internal new EnumValueConfiguration Configuration
    {
        get { return base.Configuration; }
        set { base.Configuration = value; }
    }

    public new ISortEnumValueDescriptor Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new ISortEnumValueDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new ISortEnumValueDescriptor Deprecated(string reason)
    {
        base.Deprecated(reason);
        return this;
    }

    public new ISortEnumValueDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    public new ISortEnumValueDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new ISortEnumValueDescriptor Directive<T>() where T : class, new()
    {
        base.Directive<T>();
        return this;
    }

    public new ISortEnumValueDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static SortEnumValueDescriptor New(IDescriptorContext context, string? scope, int value)
        => new(context, scope, value);
}
