using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Filters;

public class FilterInputTypeDescriptor<T>
    : FilterInputTypeDescriptor
    , IFilterInputTypeDescriptor<T>
{
    protected internal FilterInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context, entityType, scope)
    {
    }

    protected internal FilterInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context, scope)
    {
    }

    protected internal FilterInputTypeDescriptor(
        IDescriptorContext context,
        FilterInputTypeConfiguration configuration,
        string? scope)
        : base(context, configuration, scope)
    {
    }

    protected override void OnCompleteFields(
        IDictionary<string, FilterFieldConfiguration> fields,
        ISet<MemberInfo> handledProperties)
    {
        if (Configuration.Fields.IsImplicitBinding())
        {
            FieldDescriptorUtilities.AddImplicitFields(
                this,
                Configuration.EntityType!,
                p => FilterFieldDescriptor
                    .New(Context, Configuration.Scope, p)
                    .CreateConfiguration(),
                fields,
                handledProperties,
                include: (_, member)
                    => member is PropertyInfo p
                        && !handledProperties.Contains(member)
                        && !typeof(IFieldResult).IsAssignableFrom(p.PropertyType));
        }

        base.OnCompleteFields(fields, handledProperties);
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> Description(string? value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
    {
        base.BindFields(bindingBehavior);
        return this;
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly()
    {
        base.BindFieldsExplicitly();
        return this;
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly()
    {
        base.BindFieldsImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> propertyOrMember)
    {
        switch (propertyOrMember.TryExtractMember())
        {
            case PropertyInfo m:
                var fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Configuration.Member == m);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = FilterFieldDescriptor.New(Context, Configuration.Scope, m);
                    Fields.Add(fieldDescriptor);
                }

                return fieldDescriptor;

            case MethodInfo:
                throw new ArgumentException(
                    FilterInputTypeDescriptor_Field_OnlyProperties,
                    nameof(propertyOrMember));

            default:
                fieldDescriptor = FilterFieldDescriptor
                    .New(Context, Configuration.Scope, propertyOrMember);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
        }
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> Ignore(int operationId)
    {
        base.Ignore(operationId);
        return this;
    }

    /// <inheritdoc />
    public new IFilterInputTypeDescriptor<T> Ignore(string name)
    {
        base.Ignore(name);
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> propertyOrMember)
    {
        if (propertyOrMember.ExtractMember() is PropertyInfo p)
        {
            var fieldDescriptor =
                Fields.FirstOrDefault(t => t.Configuration.Member == p);

            if (fieldDescriptor is null)
            {
                fieldDescriptor =
                    IgnoreFilterFieldDescriptor.New(Context, Configuration.Scope, p);
                Fields.Add(fieldDescriptor);
            }

            return this;
        }

        throw new ArgumentException(
            FilterInputTypeDescriptor_Field_OnlyProperties,
            nameof(propertyOrMember));
    }

    public new IFilterInputTypeDescriptor<T> AllowOr(bool allow = true)
    {
        base.AllowOr(allow);
        return this;
    }

    public new IFilterInputTypeDescriptor<T> AllowAnd(bool allow = true)
    {
        base.AllowAnd(allow);
        return this;
    }

    public new IFilterInputTypeDescriptor<T> Directive<TDirective>(
        TDirective directive)
        where TDirective : class
    {
        base.Directive(directive);
        return this;
    }

    public new IFilterInputTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new IFilterInputTypeDescriptor<T> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }
}
