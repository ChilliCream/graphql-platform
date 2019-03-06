using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
    {
        public ObjectTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                AddImplicitFields(fields, handledMembers);
            }

            base.OnCompleteFields(fields, handledMembers);
        }

        private void AddImplicitFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.ClrType != typeof(object))
            {
                foreach (MemberInfo member in
                    Context.Inspector.GetMembers(Definition.ClrType))
                {
                    ObjectFieldDefinition fieldDefinition =
                        ObjectFieldDescriptor
                            .New(Context, member)
                            .CreateDefinition();

                    if (!fields.ContainsKey(fieldDefinition.Name))
                    {
                        fields[fieldDefinition.Name] = fieldDefinition;
                    }
                }
            }
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
            base.Interface<TInterface>(type);
            return this;
        }

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
            return Field(propertyOrMethod);
        }

        public new IObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            base.Directive<TDirective>(directive);
            return this;
        }

        public new IObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>(new TDirective());
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
