﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

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
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Interface);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Interface);
        }

        protected InterfaceTypeDescriptor(
            IDescriptorContext context)
            : base(context)
        {
            Definition.ClrType = typeof(object);
        }

        internal protected override InterfaceTypeDefinition Definition { get; } =
            new InterfaceTypeDefinition();

        protected ICollection<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected override void OnCreateDefinition(
            InterfaceTypeDefinition definition)
        {
            if (Definition.ClrType is { })
            {
                Context.Inspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.ClrType);
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

            Definition.Interfaces.Add(typeof(TInterface).GetOutputType());
            return this;
        }

        public IInterfaceTypeDescriptor Interface<TInterface>(
            TInterface type)
            where TInterface : InterfaceType
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Definition.Interfaces.Add(new SchemaTypeReference(type));
            return this;
        }

        public IInterfaceTypeDescriptor Interface(
            NamedTypeNode namedType)
        {
            if (namedType == null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            Definition.Interfaces.Add(new SyntaxTypeReference(
                namedType, TypeContext.Output));
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
            if (fieldDescriptor is { })
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
            Definition.AddDirective(directiveInstance);
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

        public static InterfaceTypeDescriptor FromSchemaType(
            IDescriptorContext context, Type schemaType)
        {
            var descriptor = New(context, schemaType);
            descriptor.Definition.ClrType = typeof(object);
            return descriptor;
        }
    }
}
