using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal class InterfaceTypeDescriptor<T>
        : InterfaceTypeDescriptor
        , IInterfaceTypeDescriptor<T>
    {
        public InterfaceTypeDescriptor()
            : base(typeof(T))
        {
        }

        protected override void OnCompleteFields(
            IDictionary<string, InterfaceFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (InterfaceDescription.FieldBindingBehavior ==
                BindingBehavior.Implicit)
            {
                AddImplicitFields(fields, handledMembers);
            }
        }

        private void AddImplicitFields(
            IDictionary<string, InterfaceFieldDescription> fields,
            ICollection<MemberInfo> handledMembers)
        {
            foreach (KeyValuePair<MemberInfo, string> member in
                GetAllMembers(handledMembers))
            {
                if (!fields.ContainsKey(member.Value))
                {
                    var fieldDescriptor = new InterfaceFieldDescriptor(
                        member.Key);

                    fields[member.Value] = fieldDescriptor
                        .CreateDescription();
                }
            }
        }

        private Dictionary<MemberInfo, string> GetAllMembers(
            ICollection<MemberInfo> handledMembers)
        {
            var members = new Dictionary<MemberInfo, string>();

            foreach (KeyValuePair<string, MemberInfo> member in
                ReflectionUtils.GetMembers(InterfaceDescription.ClrType))
            {
                if (!handledMembers.Contains(member.Value))
                {
                    members[member.Value] = member.Key;
                }
            }

            return members;
        }

        protected InterfaceFieldDescriptor Field<TSource>(
            Expression<Func<TSource, object>> propertyOrMethod)
        {
            if (propertyOrMethod == null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                var fieldDescriptor = new InterfaceFieldDescriptor(member);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(propertyOrMethod));
        }

        #region IInterfaceTypeDescriptor<T>

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Name(
            NameString name)
        {
            Name(name);
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.BindFields(
            BindingBehavior bindingBehavior)
        {
            InterfaceDescription.FieldBindingBehavior = bindingBehavior;
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceTypeDescriptor<T>.Field(
            Expression<Func<T, object>> propertyOrMethod)
        {
            return Field(propertyOrMethod);
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>
            .ResolveAbstractType(ResolveAbstractType resolveAbstractType)
        {
            ResolveAbstractType(resolveAbstractType);
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Directive<TD>(
            TD directive)
        {
            InterfaceDescription.Directives.AddDirective(directive);
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Directive<TD>()
        {
            InterfaceDescription.Directives.AddDirective(new TD());
            return this;
        }

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            InterfaceDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
