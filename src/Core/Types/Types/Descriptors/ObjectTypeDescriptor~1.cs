using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
        , IHasClrType
    {

        public ObjectTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
        }

        Type IHasClrType.ClrType => Definition.ClrType;

        protected override void OnCompleteFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    p => ObjectFieldDescriptor
                        .New(Context, p)
                        .CreateDefinition(),
                    fields,
                    handledMembers);
            }

            base.OnCompleteFields(fields, handledMembers);
        }

        public new IObjectTypeDescriptor<T> Name(NameString name)
        {
            base.Name(name);
            return this;
        }

        public new IObjectTypeDescriptor<T> Description(
            string description)
        {
            base.Description(description);
            return this;
        }

        public IObjectTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public new IObjectTypeDescriptor<T> Interface<TInterface>()
            where TInterface : InterfaceType
        {
            base.Interface<TInterface>();
            return this;
        }

        public new IObjectTypeDescriptor<T> Interface<TInterface>(
            TInterface type)
            where TInterface : InterfaceType
        {
            base.Interface(type);
            return this;
        }

        public new IObjectTypeDescriptor<T> Implements<TInterface>()
            where TInterface : InterfaceType =>
            Interface<TInterface>();

        public new IObjectTypeDescriptor<T> Interface(NamedTypeNode type)
        {
            base.Interface(type);
            return this;
        }

        public new IObjectTypeDescriptor<T> Implements<TInterface>(TInterface type)
            where TInterface : InterfaceType =>
            Interface(type);

        public new IObjectTypeDescriptor<T> Implements(NamedTypeNode type) =>
            Interface(type);

        public new IObjectTypeDescriptor<T> Include<TResolver>()
        {
            base.Include<TResolver>();
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
            TDirective directive)
            where TDirective : class
        {
            base.Directive(directive);
            return this;
        }

        public new IObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive(new TDirective());
            return this;
        }

        public new IObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
