using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class EnumTypeDescriptor
        : DescriptorBase<EnumTypeDefinition>
        , IEnumTypeDescriptor
    {
        protected EnumTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        protected EnumTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context)
        {
            Definition.ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
            Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        internal protected override EnumTypeDefinition Definition { get; } =
            new EnumTypeDefinition();

        protected ICollection<EnumValueDescriptor> Values { get; } =
            new List<EnumValueDescriptor>();

        protected override void OnCreateDefinition(
            EnumTypeDefinition definition)
        {
            if (Definition.ClrType is { })
            {
                Context.Inspector.ApplyAttributes(this, Definition.ClrType);
            }

            var values = Values.Select(t => t.CreateDefinition()).ToDictionary(t => t.Value);
            AddImplicitValues(definition, values);

            definition.Values.Clear();

            foreach (EnumValueDefinition value in values.Values)
            {
                definition.Values.Add(value);
            }
        }

        protected void AddImplicitValues(
            EnumTypeDefinition typeDefinition,
            IDictionary<object, EnumValueDefinition> values)
        {
            if (typeDefinition.Values.IsImplicitBinding())
            {
                foreach (object value in Context.Inspector.GetEnumValues(typeDefinition.ClrType))
                {
                    EnumValueDefinition valueDefinition =
                        EnumValueDescriptor.New(Context, value)
                            .CreateDefinition();

                    if (!values.ContainsKey(valueDefinition.Value))
                    {
                        values.Add(valueDefinition.Value, valueDefinition);
                    }
                }
            }
        }

        public IEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition)
        {
            Definition.SyntaxNode = enumTypeDefinition;
            return this;
        }

        public IEnumTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IEnumTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IEnumTypeDescriptor BindItems(
            BindingBehavior behavior) =>
            BindValues(behavior);

        public IEnumTypeDescriptor BindValues(
            BindingBehavior behavior)
        {
            Definition.Values.BindingBehavior = behavior;
            return this;
        }

        public IEnumTypeDescriptor BindValuesExplicitly() =>
            BindValues(BindingBehavior.Explicit);

        public IEnumTypeDescriptor BindValuesImplicitly() =>
            BindValues(BindingBehavior.Implicit);

        public IEnumValueDescriptor Item<T>(T value)
        {
            EnumValueDescriptor descriptor = Values.FirstOrDefault(t =>
                t.Definition.Value.Equals(value));
            if (descriptor is { })
            {
                return descriptor;
            }

            descriptor = new EnumValueDescriptor(Context, value);
            Values.Add(descriptor);
            return descriptor;
        }

        public IEnumValueDescriptor Value<T>(T value) => Item<T>(value);

        public IEnumTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IEnumTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IEnumTypeDescriptor Directive(
            NameString name, params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static EnumTypeDescriptor New(
            IDescriptorContext context) =>
            new EnumTypeDescriptor(context);

        public static EnumTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new EnumTypeDescriptor(context, clrType);

        public static EnumTypeDescriptor<T> New<T>(
            IDescriptorContext context) =>
            new EnumTypeDescriptor<T>(context);

        public static EnumTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType)
        {
            var descriptor = New(context, schemaType);
            descriptor.Definition.ClrType = typeof(object);
            return descriptor;
        }
    }
}
