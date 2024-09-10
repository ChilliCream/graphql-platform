using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors;

public class InputObjectTypeDescriptor<T>
    : InputObjectTypeDescriptor
    , IInputObjectTypeDescriptor<T>
    , IHasRuntimeType
{
    protected internal InputObjectTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected internal InputObjectTypeDescriptor(
        IDescriptorContext context,
        InputObjectTypeDefinition definition)
        : base(context, definition)
    {
    }

    Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

    protected override void OnCompleteFields(
        IDictionary<string, InputFieldDefinition> fields,
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
        Definition.Fields.BindingBehavior = behavior;
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
            var fieldDescriptor = Fields.FirstOrDefault(t => t.Definition.Property == p);

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
