using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
        , IDescriptionFactory<InterfaceTypeDescription>
    {
        public InterfaceTypeDescriptor()
        {
        }

        public InterfaceTypeDescriptor(Type clrType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            InterfaceDescription.Name = clrType.GetGraphQLName();
            InterfaceDescription.Description = clrType.GetGraphQLDescription();
            InterfaceDescription.ClrType = clrType;
        }

        protected List<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected InterfaceTypeDescription InterfaceDescription { get; } =
            new InterfaceTypeDescription();

        public InterfaceTypeDescription CreateDescription()
        {
            CompleteFields();
            return InterfaceDescription;
        }

        protected virtual void CompleteFields()
        {
            var fields = new Dictionary<string, InterfaceFieldDescription>();
            var handledMembers = new HashSet<MemberInfo>();

            foreach (InterfaceFieldDescriptor fieldDescriptor in Fields)
            {
                InterfaceFieldDescription fieldDescription = fieldDescriptor
                    .CreateDescription();

                if (!fieldDescription.Ignored)
                {
                    fields[fieldDescription.Name] = fieldDescription;
                }

                if (fieldDescription.ClrMember != null)
                {
                    handledMembers.Add(fieldDescription.ClrMember);
                }
            }

            OnCompleteFields(fields, handledMembers);

            InterfaceDescription.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<string, InterfaceFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
        }

        protected void SyntaxNode(InterfaceTypeDefinitionNode syntaxNode)
        {
            InterfaceDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            InterfaceDescription.Name = name.EnsureNotEmpty(nameof(name));
        }
        protected void Description(string description)
        {
            InterfaceDescription.Description = description;
        }

        protected InterfaceFieldDescriptor Field(NameString name)
        {
            var fieldDescriptor = new InterfaceFieldDescriptor(
                name.EnsureNotEmpty(nameof(name)));
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        protected void ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            InterfaceDescription.ResolveAbstractType = resolveAbstractType
                ?? throw new ArgumentNullException(nameof(resolveAbstractType));
        }

        #region IInterfaceTypeDescriptor

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Name(NameString name)
        {
            Name(name);
            return this;
        }
        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceTypeDescriptor.Field(
            NameString name)
        {
            return Field(name);
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            ResolveAbstractType(resolveAbstractType);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive<T>(
            T directive)
        {
            InterfaceDescription.Directives.AddDirective(directive);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive<T>()
        {
            InterfaceDescription.Directives.AddDirective(new T());
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            InterfaceDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            InterfaceDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }

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

        IInterfaceTypeDescriptor<T> IInterfaceTypeDescriptor<T>.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            InterfaceDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
