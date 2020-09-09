using System;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortEnumTypeDescriptor
        : EnumTypeDescriptor
        , ISortEnumTypeDescriptor
    {
        protected SortEnumTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
        }

        protected SortEnumTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context, clrType)
        {
        }

        protected SortEnumTypeDescriptor(
            IDescriptorContext context,
            EnumTypeDefinition definition)
            : base(context, definition)
        {
        }

        protected internal new EnumTypeDefinition Definition
        {
            get { return base.Definition; }
            set { base.Definition = value; }
        }


        public new ISortEnumTypeDescriptor SyntaxNode(EnumTypeDefinitionNode enumTypeDefinition)
        {
            base.SyntaxNode(enumTypeDefinition);
            return this;
        }

        public new ISortEnumTypeDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortEnumTypeDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public ISortEnumValueDescriptor Operation(int operation)
        {
            SortEnumValueDescriptor? descriptor = Values
                .OfType<SortEnumValueDescriptor>()
                .FirstOrDefault(
                    t =>
                        t.Definition.Value is not null &&
                        t.Definition.Value.Equals(operation));

            if (descriptor is not null)
            {
                return descriptor;
            }

            descriptor = SortEnumValueDescriptor.New(Context, operation);
            Values.Add(descriptor);
            return descriptor;
        }

        public new ISortEnumTypeDescriptor Directive<T>(T directiveInstance) where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortEnumTypeDescriptor Directive<T>() where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new ISortEnumTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static new SortEnumTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new SortEnumTypeDescriptor(context, clrType);

        public static new SortEnumTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType)
        {
            SortEnumTypeDescriptor descriptor = New(context, schemaType);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static new SortEnumTypeDescriptor From(
            IDescriptorContext context,
            EnumTypeDefinition definition ) =>
            new SortEnumTypeDescriptor(context, definition);
    }
}
