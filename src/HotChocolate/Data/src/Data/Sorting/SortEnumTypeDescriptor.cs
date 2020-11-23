using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    public class SortEnumTypeDescriptor
        : DescriptorBase<SortEnumTypeDefinition>,
          ISortEnumTypeDescriptor
    {
        protected SortEnumTypeDescriptor(
            IDescriptorContext context,
            Type clrType,
            string? scope)
            : base(context)
        {
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
            Definition.EntityType = clrType;
            Definition.RuntimeType = typeof(object);
            Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
            Definition.Scope = scope;
        }

        protected SortEnumTypeDescriptor(
            IDescriptorContext context,
            SortEnumTypeDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected override SortEnumTypeDefinition Definition { get; set; } =
            new SortEnumTypeDefinition();

        protected ICollection<SortEnumValueDescriptor> Values { get; } =
            new List<SortEnumValueDescriptor>();

        protected override void OnCreateDefinition(
            SortEnumTypeDefinition definition)
        {
            if (Definition.RuntimeType is { })
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.RuntimeType);
            }

            var values = Values.Select(t => t.CreateDefinition())
                .OfType<SortEnumValueDefinition>()
                .ToDictionary(t => t.Value);

            definition.Values.Clear();

            foreach (SortEnumValueDefinition value in values.Values)
            {
                definition.Values.Add(value);
            }

            base.OnCreateDefinition(definition);
        }

        public ISortEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition)
        {
            Definition.SyntaxNode = enumTypeDefinition;
            return this;
        }

        public ISortEnumTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public ISortEnumTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public ISortEnumValueDescriptor Operation(int operation)
        {
            SortEnumValueDescriptor? descriptor = Values
                .FirstOrDefault(
                    t =>
                        t.Definition.Value is not null &&
                        t.Definition.Value.Equals(operation));

            if (descriptor is not null)
            {
                return descriptor;
            }

            descriptor = SortEnumValueDescriptor.New(Context, Definition.Scope, operation);
            Values.Add(descriptor);
            return descriptor;
        }

        public ISortEnumTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public ISortEnumTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
            return this;
        }

        public ISortEnumTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static SortEnumTypeDescriptor New(
            IDescriptorContext context,
            Type type,
            string? scope) =>
            new SortEnumTypeDescriptor(context, type, scope);

        public static SortEnumTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType,
            string? scope)
        {
            SortEnumTypeDescriptor descriptor = New(context, schemaType, scope);
            return descriptor;
        }

        public static SortEnumTypeDescriptor From(
            IDescriptorContext context,
            SortEnumTypeDefinition definition) =>
            new SortEnumTypeDescriptor(context, definition);
    }
}
