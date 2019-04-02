using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;

namespace HotChocolate.Types.Descriptors
{
    public class InputObjectTypeDescriptor
        : DescriptorBase<InputObjectTypeDefinition>
        , IInputObjectTypeDescriptor
    {
        public InputObjectTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(
                clrType, TypeKind.InputObject);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.InputObject);
        }

        public InputObjectTypeDescriptor(
            IDescriptorContext context,
            NameString name)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        public InputObjectTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.ClrType = typeof(object);
        }

        protected override InputObjectTypeDefinition Definition { get; } =
            new InputObjectTypeDefinition();

        protected List<InputFieldDescriptor> Fields { get; } =
            new List<InputFieldDescriptor>();

        protected override void OnCreateDefinition(
            InputObjectTypeDefinition definition)
        {
            var fields = new Dictionary<NameString, InputFieldDefinition>();
            var handledProperties = new HashSet<PropertyInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Property,
                fields,
                handledProperties);

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, InputFieldDefinition> fields,
            ISet<PropertyInfo> handledMembers)
        {
        }

        public IInputObjectTypeDescriptor SyntaxNode(
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode)
        {
            Definition.SyntaxNode = inputObjectTypeDefinitionNode;
            return this;
        }

        public IInputObjectTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IInputObjectTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IInputFieldDescriptor Field(NameString name)
        {
            var field = new InputFieldDescriptor(
                Context,
                name.EnsureNotEmpty(nameof(name)));
            Fields.Add(field);
            return field;
        }

        public IInputObjectTypeDescriptor Directive<T>(T directive)
            where T : class
        {
            Definition.AddDirective(directive);
            return this;
        }

        public IInputObjectTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IInputObjectTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static InputObjectTypeDescriptor New(
            IDescriptorContext context,
            NameString name) =>
            new InputObjectTypeDescriptor(context, name);

        public static InputObjectTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new InputObjectTypeDescriptor(context, clrType);

        public static InputObjectTypeDescriptor<T> New<T>(
            IDescriptorContext context) =>
            new InputObjectTypeDescriptor<T>(context);
    }
}
