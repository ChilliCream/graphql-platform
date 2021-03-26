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
            Definition.RuntimeType = typeof(object);
            Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        protected EnumTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context)
        {
            Definition.RuntimeType = clrType ?? throw new ArgumentNullException(nameof(clrType));
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
            Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        protected EnumTypeDescriptor(IDescriptorContext context, EnumTypeDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal override EnumTypeDefinition Definition { get; protected set; } =
            new EnumTypeDefinition();

        protected ICollection<EnumValueDescriptor> Values { get; } =
            new List<EnumValueDescriptor>();

        protected override void OnCreateDefinition(
            EnumTypeDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.RuntimeType);
                Definition.AttributesAreApplied = true;
            }

            var values = Values.Select(t => t.CreateDefinition()).ToDictionary(t => t.Value);
            AddImplicitValues(definition, values);

            definition.Values.Clear();

            foreach (EnumValueDefinition value in values.Values)
            {
                definition.Values.Add(value);
            }

            base.OnCreateDefinition(definition);
        }

        protected void AddImplicitValues(
            EnumTypeDefinition typeDefinition,
            IDictionary<object, EnumValueDefinition> values)
        {
            if (typeDefinition.Values.IsImplicitBinding())
            {
                foreach (object value in Context.TypeInspector.GetEnumValues(typeDefinition.RuntimeType))
                {
                    EnumValueDefinition valueDefinition =
                        EnumValueDescriptor.New(Context, value)
                            .CreateDefinition();

                    if (valueDefinition.Value is not null &&
                        !values.ContainsKey(valueDefinition.Value))
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
                t.Definition.Value is not null &&
                t.Definition.Value.Equals(value));

            if (descriptor is not null)
            {
                return descriptor;
            }

            descriptor = EnumValueDescriptor.New(Context, value);
            Values.Add(descriptor);
            return descriptor;
        }

        public IEnumValueDescriptor Value<T>(T value) => Item<T>(value);

        public IEnumTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public IEnumTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
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
            EnumTypeDescriptor descriptor = New(context, schemaType);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static EnumTypeDescriptor From(
            IDescriptorContext context,
            EnumTypeDefinition definition) =>
            new EnumTypeDescriptor(context, definition);

        public static EnumTypeDescriptor From<T>(
            IDescriptorContext context,
            EnumTypeDefinition definition) =>
            new EnumTypeDescriptor<T>(context, definition);
    }
}
