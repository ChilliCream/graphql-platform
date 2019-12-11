using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class SchemaTypeDescriptor
        : DescriptorBase<SchemaTypeDefinition>
        , ISchemaTypeDescriptor
    {
        public SchemaTypeDescriptor(IDescriptorContext context, Type type)
            : base(context)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            Definition.Name = context.Naming.GetTypeName(type);
        }

        internal protected override SchemaTypeDefinition Definition { get; } =
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
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public ISchemaTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
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
    }
}
