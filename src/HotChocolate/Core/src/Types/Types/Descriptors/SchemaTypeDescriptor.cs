using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class SchemaTypeDescriptor
        : DescriptorBase<SchemaTypeDefinition>
        , ISchemaTypeDescriptor
    {
        protected SchemaTypeDescriptor(IDescriptorContext context, Type type)
            : base(context)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            Definition.Name = context.Naming.GetTypeName(type);
        }

        protected SchemaTypeDescriptor(
            IDescriptorContext context,
            SchemaTypeDefinition definition)
            : base(context)
        {
            Definition = definition;
        }

        protected internal override SchemaTypeDefinition Definition { get; protected set; } =
            new SchemaTypeDefinition();

        public ISchemaTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public ISchemaTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public ISchemaTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public ISchemaTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
            return this;
        }

        public ISchemaTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static SchemaTypeDescriptor New(
            IDescriptorContext context,
            Type type) =>
            new SchemaTypeDescriptor(context, type);

        public static SchemaTypeDescriptor From(
            IDescriptorContext context,
            SchemaTypeDefinition definition) =>
            new SchemaTypeDescriptor(context, definition);
    }
}
