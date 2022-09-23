using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
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
        HashSet<string> subscribeResolver = null;

        if (Definition.Fields.IsImplicitBinding() &&
            Definition.FieldBindingType is not null)
        {
            var inspector = Context.TypeInspector;
            var naming = Context.Naming;
            var type = Definition.FieldBindingType;
            var isExtension = Definition.IsExtension;
            var includeStatic = (Definition.FieldBindingFlags & Static) == Static;
            var members = inspector.GetMembers(type, isExtension, includeStatic);

            foreach (var member in members)
            {
                var name = naming.GetMemberName(member, MemberKind.ObjectField);
#if NET5_0_OR_GREATER
                if(handledMembers.Add(member) &&
                    !ContainsField(GetFieldsAsSpan(), name) &&
                    IncludeField(ref subscribeResolver, members, member))
#else
                if(handledMembers.Add(member) &&
                    !ContainsField(Fields, name) &&
                    IncludeField(ref subscribeResolver, members, member))
#endif
                {
                    var descriptor = ObjectFieldDescriptor.New(
                        Context,
                        member,
                        Definition.RuntimeType,
                        type);

                    if (isExtension && inspector.IsMemberIgnored(member))
                    {
                        descriptor.Ignore();
                    }

                    Fields.Add(descriptor);
                    handledMembers.Add(member);

                    // the create definition call will trigger the OnCompleteField call
                    // on the field description and trigger the initialization of the
                    // fields arguments.
                    fields[name] = descriptor.CreateDefinition();
                }
            }
        }

        base.OnCompleteFields(fields, handledMembers);

        static bool IncludeField(
            ref HashSet<string> subscribeResolver,
            ReadOnlySpan<MemberInfo> allMembers,
            MemberInfo current)
        {
            if (subscribeResolver is null)
            {
                subscribeResolver = new HashSet<string>();

                foreach (var member in allMembers)
                {
                    HandlePossibleSubscribeMember(subscribeResolver, member);
                }
            }

            return !subscribeResolver.Contains(current.Name);
        }

#if NET5_0_OR_GREATER
        static bool ContainsField(ReadOnlySpan<ObjectFieldDescriptor> fields, string name)
#else
        static bool ContainsField(IEnumerable<ObjectFieldDescriptor> fields, string name)
#endif
        {
            foreach (var field in fields)
            {
                if (field.Definition.Name.EqualsOrdinal(name))
                {
                    return true;
                }
            }

            return false;
        }

        static void HandlePossibleSubscribeMember(
            HashSet<string> subscribeResolver,
            MemberInfo member)
        {
            if (member.IsDefined(typeof(SubscribeAttribute)))
            {
                if (member.GetCustomAttribute<SubscribeAttribute>() is { With: not null } attr)
                {
                    subscribeResolver.Add(attr.With);
                }
            }
        }
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

    [Obsolete("Use Implements.")]
    public new IObjectTypeDescriptor<T> Interface<TInterface>()
        where TInterface : InterfaceType
        => Implements<TInterface>();

    [Obsolete("Use Implements.")]
    public new IObjectTypeDescriptor<T> Interface<TInterface>(TInterface type)
        where TInterface : InterfaceType
        => Implements(type);

    [Obsolete("Use Implements.")]
    public new IObjectTypeDescriptor<T> Interface(NamedTypeNode type)
        => Implements(type);

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
    {
        return base.Field(propertyOrMethod);
    }

    public IObjectFieldDescriptor Field<TValue>(
        Expression<Func<T, TValue>> propertyOrMethod)
    {
        return base.Field(propertyOrMethod);
    }

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

