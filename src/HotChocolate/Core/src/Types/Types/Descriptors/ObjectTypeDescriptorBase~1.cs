using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptorBase<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
        , IHasRuntimeType
    {
        protected internal ObjectTypeDescriptorBase(
            IDescriptorContext context,
            Type clrType)
            : base(context, clrType)
        {
        }

        protected internal ObjectTypeDescriptorBase(
            IDescriptorContext context)
            : base(context)
        {
        }

        protected internal ObjectTypeDescriptorBase(
            IDescriptorContext context,
            ObjectTypeDefinition definition)
            : base(context, definition)
        {
        }

        Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

        protected override void OnCompleteFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            HashSet<string> subscribeResolver = null;

            if (Definition.Fields.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    Definition.FieldBindingType,
                    p =>
                    {
                        var descriptor = ObjectFieldDescriptor.New(
                            Context, p, Definition.RuntimeType, Definition.FieldBindingType);
                        Fields.Add(descriptor);
                        return descriptor.CreateDefinition();
                    },
                    fields,
                    handledMembers,
                    include: IncludeField);
            }

            base.OnCompleteFields(fields, handledMembers);

            bool IncludeField(IReadOnlyList<MemberInfo> all, MemberInfo current)
            {
                if (subscribeResolver is null)
                {
                    subscribeResolver = new HashSet<string>();

                    foreach(MemberInfo member in all)
                    {
                        if(member.IsDefined(typeof(SubscribeAttribute)))
                        {
                            SubscribeAttribute attribute =
                                member.GetCustomAttribute<SubscribeAttribute>();
                            if(attribute.With is not null)
                            {
                                subscribeResolver.Add(attribute.With);
                            }
                        }
                    }
                }

                return !subscribeResolver.Contains(current.Name);
            }
        }

        public new IObjectTypeDescriptor<T> Name(NameString value)
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
            Definition.Fields.BindingBehavior = behavior;
            return this;
        }

        public IObjectTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public IObjectTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public new IObjectTypeDescriptor<T> Interface<TInterface>()
            where TInterface : InterfaceType
        {
            base.Interface<TInterface>();
            return this;
        }

        public new IObjectTypeDescriptor<T> Interface<TInterface>(TInterface type)
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
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
