using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors;

public class InterfaceTypeDescriptor<T>
    : InterfaceTypeDescriptor
    , IInterfaceTypeDescriptor<T>
    , IHasRuntimeType
{
    protected internal InterfaceTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected internal InterfaceTypeDescriptor(
        IDescriptorContext context,
        InterfaceTypeDefinition definition)
        : base(context, definition)
    {
    }

    Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

    protected override void OnCompleteFields(
        IDictionary<string, InterfaceFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    {
        if (Definition.Fields.IsImplicitBinding())
        {
            FieldDescriptorUtilities.AddImplicitFields(
                this,
                p => InterfaceFieldDescriptor.New(Context, p).CreateDefinition(),
                fields,
                handledMembers);
        }

        base.OnCompleteFields(fields, handledMembers);
    }

    public new IInterfaceTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Description(string value)
    {
        base.Description(value);
        return this;
    }

    public IInterfaceTypeDescriptor<T> BindFields(
        BindingBehavior behavior)
    {
        Definition.Fields.BindingBehavior = behavior;
        return this;
    }

    public IInterfaceTypeDescriptor<T> BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    public IInterfaceTypeDescriptor<T> BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    public new IInterfaceTypeDescriptor<T> Implements<TInterface>()
        where TInterface : InterfaceType
    {
        base.Implements<TInterface>();
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Implements<TInterface>(TInterface type)
        where TInterface : InterfaceType
    {
        base.Implements(type);
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Implements(NamedTypeNode type)
    {
        base.Implements(type);
        return this;
    }

    public IInterfaceFieldDescriptor Field(
        Expression<Func<T, object>> propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        var member = propertyOrMethod.ExtractMember();
        if (member is PropertyInfo or MethodInfo)
        {
            var fieldDescriptor = Fields.FirstOrDefault(t => t.Definition.Member == member);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = new InterfaceFieldDescriptor(Context, member);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(
            InterfaceTypeDescriptor_MustBePropertyOrMethod,
            nameof(propertyOrMethod));
    }

    public IInterfaceFieldDescriptor Field(MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is not { MemberType: MemberTypes.Property or MemberTypes.Method })
        {
            throw new ArgumentException(
                InterfaceTypeDescriptor_MustBePropertyOrMethod,
                nameof(propertyOrMethod));
        }

        var fieldDescriptor = new InterfaceFieldDescriptor(Context, propertyOrMethod);
        Fields.Add(fieldDescriptor);
        return fieldDescriptor;
    }

    public new IInterfaceTypeDescriptor<T> ResolveAbstractType(
        ResolveAbstractType typeResolver)
    {
        base.ResolveAbstractType(typeResolver);
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new IInterfaceTypeDescriptor<T> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }
}
