using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceTypeDescriptor
        : DescriptorBase<InterfaceTypeDefinition>
        , IInterfaceTypeDescriptor
    {
        protected InterfaceTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType is null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.RuntimeType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Interface);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Interface);
        }

        protected InterfaceTypeDescriptor(
            IDescriptorContext context)
            : base(context)
        {
            Definition.RuntimeType = typeof(object);
        }

        protected InterfaceTypeDescriptor(
            IDescriptorContext context,
            InterfaceTypeDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal override InterfaceTypeDefinition Definition { get; protected set; } =
            new InterfaceTypeDefinition();

        protected ICollection<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected override void OnCreateDefinition(
            InterfaceTypeDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.RuntimeType);
                Definition.AttributesAreApplied = true;
            }

            var fields = new Dictionary<NameString, InterfaceFieldDefinition>();
            var handledMembers = new HashSet<MemberInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Member,
                fields,
                handledMembers);

            OnCompleteFields(fields, handledMembers);

            Definition.Fields.AddRange(fields.Values);

            base.OnCreateDefinition(definition);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, InterfaceFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
        }

        public IInterfaceTypeDescriptor SyntaxNode(
            InterfaceTypeDefinitionNode interfaceTypeDefinition)
        {
            Definition.SyntaxNode = interfaceTypeDefinition;
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

        public IInterfaceTypeDescriptor Interface<TInterface>()
            where TInterface : InterfaceType
        {
            if (typeof(TInterface) == typeof(InterfaceType))
            {
                throw new ArgumentException(
                    TypeResources.InterfaceTypeDescriptor_InterfaceBaseClass);
            }

            Definition.Interfaces.Add(
                Context.TypeInspector.GetTypeRef(typeof(TInterface), TypeContext.Output));
            return this;
        }

        public IInterfaceTypeDescriptor Interface<TInterface>(
            TInterface type)
            where TInterface : InterfaceType
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Definition.Interfaces.Add(new SchemaTypeReference(type));
            return this;
        }

        public IInterfaceTypeDescriptor Interface(
            NamedTypeNode namedType)
        {
            if (namedType is null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            Definition.Interfaces.Add(TypeReference.Create(namedType, TypeContext.Output));
            return this;
        }

        public IInterfaceTypeDescriptor Implements<T>()
            where T : InterfaceType =>
            Interface<T>();

        public IInterfaceTypeDescriptor Implements<T>(T type)
            where T : InterfaceType =>
            Interface(type);

        public IInterfaceTypeDescriptor Implements(NamedTypeNode type) =>
            Interface(type);

        public IInterfaceFieldDescriptor Field(NameString name)
        {
            InterfaceFieldDescriptor fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name.Equals(name));

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = InterfaceFieldDescriptor.New(
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

        public IInterfaceTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public IInterfaceTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
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

        public static InterfaceTypeDescriptor FromSchemaType(
            IDescriptorContext context, Type schemaType)
        {
            InterfaceTypeDescriptor descriptor = New(context, schemaType);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static InterfaceTypeDescriptor From(
            IDescriptorContext context,
            InterfaceTypeDefinition definition) =>
            new InterfaceTypeDescriptor(context, definition);

        public static InterfaceTypeDescriptor<T> From<T>(
            IDescriptorContext context,
            InterfaceTypeDefinition definition) =>
            new InterfaceTypeDescriptor<T>(context, definition);
    }
}
