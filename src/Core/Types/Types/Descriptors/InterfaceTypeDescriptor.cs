using System;
using System.Collections.Generic;
using System.Linq;
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
            Definition.Name =
                context.Naming.GetTypeName(clrType, TypeKind.Interface);
            Definition.Description =
                context.Naming.GetTypeDescription(clrType, TypeKind.Interface);
        }

        public InterfaceTypeDescriptor(
            IDescriptorContext context,
            NameString name)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected override InterfaceTypeDefinition Definition { get; } =
            new InterfaceTypeDefinition();

        protected List<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected override void OnCreateDefinition(
            InterfaceTypeDefinition definition)
        {
            var fields = new Dictionary<NameString, InterfaceFieldDefinition>();
            var handledMembers = new HashSet<MemberInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Member,
                fields,
                handledMembers);

            OnCompleteFields(fields, handledMembers);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, InterfaceFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
        }

        public IInterfaceTypeDescriptor SyntaxNode(
            InterfaceTypeDefinitionNode interfaceTypeDefinitionNode)
        {
            Definition.SyntaxNode = interfaceTypeDefinitionNode;
            return this;
        }

        public IInterfaceTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IInterfaceTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IInterfaceFieldDescriptor Field(NameString name)
        {
            var fieldDescriptor = new InterfaceFieldDescriptor(
                Context,
                name.EnsureNotEmpty(nameof(name)));
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        public IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType typeResolver)
        {
            Definition.ResolveAbstractType = typeResolver
                ?? throw new ArgumentNullException(nameof(typeResolver));
            return this;
        }

        public IInterfaceTypeDescriptor Directive<T>(T directive)
            where T : class
        {
            Definition.AddDirective(directive);
            return this;
        }

        public IInterfaceTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IInterfaceTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }
    }
}
