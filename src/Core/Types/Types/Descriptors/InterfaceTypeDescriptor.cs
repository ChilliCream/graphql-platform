using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceTypeDescriptor
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
            IDescriptorContext context)
            : base(context)
        {
            Definition.ClrType = typeof(object);
        }

        protected override InterfaceTypeDefinition Definition { get; } =
            new InterfaceTypeDefinition();

        protected ICollection<InterfaceFieldDescriptor> Fields { get; } =
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

        public static InterfaceTypeDescriptor New(
            IDescriptorContext context) =>
            new InterfaceTypeDescriptor(context);

        public static InterfaceTypeDescriptor New(
            IDescriptorContext context, Type clrType) =>
            new InterfaceTypeDescriptor(context, clrType);

        public static InterfaceTypeDescriptor<T> New<T>(
            IDescriptorContext context) =>
            new InterfaceTypeDescriptor<T>(context);
    }
}
