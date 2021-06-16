using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public abstract class ObjectTypeDescriptorBase<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
        , IHasRuntimeType
    {
        protected ObjectTypeDescriptorBase(
            IDescriptorContext context,
            Type clrType)
            : base(context, clrType)
        {
        }

        protected ObjectTypeDescriptorBase(
            IDescriptorContext context)
            : base(context)
        {
        }

        protected ObjectTypeDescriptorBase(
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

            if (Definition.Fields.IsImplicitBinding() &&
                Definition.FieldBindingType is not null)
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    Definition.FieldBindingType,
                    p =>
                    {
                        var descriptor = ObjectFieldDescriptor.New(
                            Context, p, Definition.RuntimeType, Definition.FieldBindingType);

                        if (Definition.IsExtension && Context.TypeInspector.IsMemberIgnored(p))
                        {
                            descriptor.Ignore();
                        }

                        Fields.Add(descriptor);
                        return descriptor.CreateDefinition();
                    },
                    fields,
                    handledMembers,
                    include: IncludeField,
                    includeIgnoredMembers
                    : Definition.IsExtension);
            }

            base.OnCompleteFields(fields, handledMembers);

            bool IncludeField(IReadOnlyList<MemberInfo> all, MemberInfo current)
            {
                NameString name = Context.Naming.GetMemberName(current, MemberKind.ObjectField);

                if (Fields.Any(t => t.Definition.Name.Equals(name)))
                {
                    return false;
                }

                if (subscribeResolver is null)
                {
                    subscribeResolver = new HashSet<string>();

                    foreach (MemberInfo member in all)
                    {
                        HandlePossibleSubscribeMember(member);
                    }
                }

                return !subscribeResolver.Contains(current.Name);
            }

            void HandlePossibleSubscribeMember(MemberInfo member)
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

        [Obsolete("Use ObjectTypeExtension API.")]
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
