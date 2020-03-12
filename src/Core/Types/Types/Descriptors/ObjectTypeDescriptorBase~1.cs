using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptorBase<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
        , IHasClrType
    {
        public ObjectTypeDescriptorBase(IDescriptorContext context, Type clrType)
            : base(context, clrType)
        {
        }

        public ObjectTypeDescriptorBase(IDescriptorContext context)
            : base(context)
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
                    Definition.FieldBindingType,
                    p =>
                    {
                        ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(
                            Context, p, Definition.FieldBindingType);
                        Fields.Add(descriptor);
                        return descriptor.CreateDefinition();
                    },
                    fields,
                    handledMembers);
            }

            BindSubscribeResolver(fields);

            base.OnCompleteFields(fields, handledMembers);
        }

        private void BindSubscribeResolver(IDictionary<NameString, ObjectFieldDefinition> fields)
        {
            foreach (var item in fields.ToList())
            {
                if (item.Value.Member is { }
                    && item.Value.Member.IsDefined(typeof(SubscribeAttribute), true))
                {
                    SubscribeAttribute subscribe =
                        item.Value.Member.GetCustomAttribute<SubscribeAttribute>(true);
                    fields.Remove(item.Key);

                    var field = fields.FirstOrDefault(t =>
                        t.Value.Member.Name == subscribe.ResolverName);
                    field.Value.SubscribeResolver = ResolverCompiler.Subscribe.Compile(
                        item.Value.SourceType, item.Value.ResolverType, item.Value.Member);
                }
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
