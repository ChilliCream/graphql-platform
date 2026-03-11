using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors;

public class InputObjectTypeDescriptor<T>
    : InputObjectTypeDescriptor
    , IInputObjectTypeDescriptor<T>
    , IRuntimeTypeProvider
{
    protected internal InputObjectTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected internal InputObjectTypeDescriptor(
        IDescriptorContext context,
        InputObjectTypeConfiguration definition)
        : base(context, definition)
    {
    }

    Type IRuntimeTypeProvider.RuntimeType => Configuration.RuntimeType;

    protected override void OnCompleteFields(
        IDictionary<string, InputFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers)
    {
        InferFieldsFromFieldBindingType(fields, handledMembers);
        base.OnCompleteFields(fields, handledMembers);
    }

    public new IInputObjectTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new IInputObjectTypeDescriptor<T> Description(string value)
    {
        base.Description(value);
        return this;
    }

    public IInputObjectTypeDescriptor<T> BindFields(
        BindingBehavior behavior)
    {
        Configuration.Fields.BindingBehavior = behavior;
        return this;
    }

    public IInputObjectTypeDescriptor<T> BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    public IInputObjectTypeDescriptor<T> BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    public IInputFieldDescriptor Field<TValue>(
        Expression<Func<T, TValue>> property)
    {
        if (property.ExtractMember() is PropertyInfo p)
        {
            var fieldDescriptor = Fields.FirstOrDefault(t => t.Configuration.Property == p);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = new InputFieldDescriptor(Context, p);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(InputObjectTypeDescriptor_OnlyProperties, nameof(property));
    }

    public new IInputObjectTypeDescriptor<T> Directive<TDirective>(
        TDirective directive)
        where TDirective : class
    {
        base.Directive(directive);
        return this;
    }

    public new IInputObjectTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive(new TDirective());
        return this;
    }

    public new IInputObjectTypeDescriptor<T> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }
}
