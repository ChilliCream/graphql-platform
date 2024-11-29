using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.FieldBindingFlags;

namespace HotChocolate.Types.Descriptors;

public abstract class ObjectTypeDescriptorBase<T>
    : ObjectTypeDescriptor
    , IObjectTypeDescriptor<T>
    , IHasRuntimeType
{
    protected ObjectTypeDescriptorBase(
        IDescriptorContext context,
        Type clrType)
        : base(context, clrType) { }

    protected ObjectTypeDescriptorBase(
        IDescriptorContext context)
        : base(context) { }

    protected ObjectTypeDescriptorBase(
        IDescriptorContext context,
        ObjectTypeDefinition definition)
        : base(context, definition) { }

    Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

    protected override void OnCompleteFields(
        IDictionary<string, ObjectFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    {
        InferFieldsFromFieldBindingType(fields, handledMembers);
        base.OnCompleteFields(fields, handledMembers);
    }

    public new IObjectTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new IObjectTypeDescriptor<T> Description(
        string value)
    {
        base.Description(value);
        return this;
    }

    public IObjectTypeDescriptor<T> BindFields(
        BindingBehavior behavior)
    {
        if (behavior == Definition.Fields.BindingBehavior)
        {
            // nothing changed so we just return!
            return this;
        }

        if (behavior == BindingBehavior.Explicit)
        {
            Definition.Fields.BindingBehavior = BindingBehavior.Explicit;
            Definition.FieldBindingFlags = Default;
        }
        else
        {
            Definition.Fields.BindingBehavior = BindingBehavior.Implicit;
            Definition.FieldBindingFlags = Instance;
        }

        return this;
    }

    public IObjectTypeDescriptor<T> BindFields(
        FieldBindingFlags bindingFlags)
    {
        if (bindingFlags == Definition.FieldBindingFlags)
        {
            // nothing changed so we just return!
            return this;
        }

        if (bindingFlags == Default)
        {
            Definition.Fields.BindingBehavior = BindingBehavior.Explicit;
            Definition.FieldBindingFlags = Default;
        }
        else
        {
            Definition.Fields.BindingBehavior = BindingBehavior.Implicit;
            Definition.FieldBindingFlags = bindingFlags;
        }

        return this;
    }

    public IObjectTypeDescriptor<T> BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    public IObjectTypeDescriptor<T> BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    public new IObjectTypeDescriptor<T> Implements<TInterface>()
        where TInterface : InterfaceType
    {
        base.Implements<TInterface>();
        return this;
    }

    public new IObjectTypeDescriptor<T> Implements<TInterface>(TInterface type)
        where TInterface : InterfaceType
    {
        base.Implements(type);
        return this;
    }

    public new IObjectTypeDescriptor<T> Implements(NamedTypeNode type)
    {
        base.Implements(type);
        return this;
    }

    public new IObjectTypeDescriptor<T> IsOfType(IsOfType isOfType)
    {
        base.IsOfType(isOfType);
        return this;
    }

    public IObjectFieldDescriptor Field(
        Expression<Func<T, object>> propertyOrMethod)
        => base.Field(propertyOrMethod);

    public IObjectFieldDescriptor Field<TValue>(
        Expression<Func<T, TValue>> propertyOrMethod)
        => base.Field(propertyOrMethod);

    public new IObjectTypeDescriptor<T> Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new IObjectTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive(new TDirective());
        return this;
    }

    public new IObjectTypeDescriptor<T> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }
}
