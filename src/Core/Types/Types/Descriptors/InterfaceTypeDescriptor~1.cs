using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal class InterfaceTypeDescriptor<T>
        : InterfaceTypeDescriptor
        , IInterfaceTypeDescriptor<T>
    {
        public InterfaceTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, InterfaceFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                AddImplicitFields(fields, handledMembers);
            }
        }

        private void AddImplicitFields(
            IDictionary<NameString, InterfaceFieldDefinition> fields,
            ICollection<MemberInfo> handledMembers)
        {
            if (Definition.ClrType != typeof(object))
            {
                foreach (MemberInfo member in
                    Context.Inspector.GetMembers(Definition.ClrType))
                {
                    InterfaceFieldDefinition fieldDefinition =
                        InterfaceFieldDescriptor
                            .New(Context, member)
                            .CreateDefinition();

                    if (!handledMembers.Contains(member)
                        && !fields.ContainsKey(fieldDefinition.Name))
                    {
                        handledMembers.Add(member);
                        fields[fieldDefinition.Name] = fieldDefinition;
                    }
                }
            }
        }

        public new IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode interfaceTypeDefinitionNode)
        {
            base.SyntaxNode(interfaceTypeDefinitionNode);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Name(NameString value)
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
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod)
        {
            if (propertyOrMethod == null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                var fieldDescriptor = new InterfaceFieldDescriptor(
                    Context, member);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(propertyOrMethod));
        }

        public new IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            base.ResolveAbstractType(resolveAbstractType);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class
        {
            base.Directive(directive);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
