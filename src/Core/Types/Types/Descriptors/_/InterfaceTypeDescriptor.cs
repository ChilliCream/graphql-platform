using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal class InterfaceTypeDescriptor
        : DescriptorBase<InterfaceTypeDefinition>
        , IInterfaceTypeDescriptor
    {
        public InterfaceTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType);
            Definition.Description = context.Naming.GetTypeDescription(clrType);
        }

        public InterfaceTypeDescriptor(IDescriptorContext context, NameString name)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected override InterfaceTypeDefinitionNode Definition { get; } =
            new InterfaceTypeDefinitionNode();

        protected List<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected InterfaceTypeDefinition InterfaceDescription { get; } =
            new InterfaceTypeDefinition();



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

                if (fieldDescription.Member != null)
                {
                    handledMembers.Add(fieldDescription.Member);
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

        #endregion
    }
}
