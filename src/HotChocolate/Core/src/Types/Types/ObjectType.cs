using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Types.Relay;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IComplexOutputType
        , IHasClrType
        , IHasSyntaxNode
    {
        private IReadOnlyDictionary<NameString, InterfaceType> _interfaces =

        private Action<IObjectTypeDescriptor> _configure;
        private IsOfType _isOfType;

        protected ObjectType()
        {
            _configure = Configure;
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Object;

        public ObjectTypeDefinitionNode SyntaxNode { get; private set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public IReadOnlyDictionary<NameString, InterfaceType> Interfaces =>
            _interfaces;

        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult) =>
            _isOfType(context, resolverResult);

        protected override ObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            _configure = null;
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(ObjectType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (ValidateFields(context, definition))
            {
                _isOfType = definition.IsOfType;
                SyntaxNode = definition.SyntaxNode;

                var fields = new List<ObjectField>();
                AddIntrospectionFields(context, fields);
                AddRelayNodeField(context, fields);
                fields.AddRange(definition.Fields.Select(t => new ObjectField(t)));

                Fields = new FieldCollection<ObjectField>(fields);

                CompleteInterfaces(context, definition);
                CompleteIsOfType(context);
                FieldInitHelper.CompleteFields(context, definition, Fields);
            }
        }

        private void AddIntrospectionFields(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            if (context.IsQueryType.HasValue && context.IsQueryType.Value)
            {
                fields.Add(new __SchemaField(context.DescriptorContext));
                fields.Add(new __TypeField(context.DescriptorContext));
            }

            fields.Add(new __TypeNameField(context.DescriptorContext));
        }

        private void AddRelayNodeField(
            ICompletionContext context,
            ICollection<ObjectField> fields)
        {
            if (context.IsQueryType.HasValue
                && context.IsQueryType.Value
                && context.ContextData.ContainsKey(RelayConstants.IsRelaySupportEnabled))
            {
                fields.Add(new NodeField(context.DescriptorContext));
            }
        }

        private void CompleteInterfaces(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            if (ClrType != typeof(object))
            {
                TryInferInterfaceUsageFromClrType(context, ClrType);
            }

            if (definition.KnownClrTypes.Count > 0)
            {
                definition.KnownClrTypes.Remove(typeof(object));

                foreach (Type clrType in definition.KnownClrTypes.Distinct())
                {
                    TryInferInterfaceUsageFromClrType(context, clrType);
                }
            }

            foreach (ITypeReference interfaceRef in definition.Interfaces)
            {
                if (!context.TryGetType(interfaceRef, out InterfaceType type))
                {
                    // TODO : resources
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage("COULD NOT RESOLVE INTERFACE")
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(this)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
                }

                _interfaces[type.Name] = type;
            }
        }

        private void TryInferInterfaceUsageFromClrType(
           ICompletionContext context,
           Type clrType)
        {
            foreach (Type interfaceType in clrType.GetInterfaces())
            {
                if (context.TryGetType(
                    new ClrTypeReference(interfaceType, TypeContext.Output),
                    out InterfaceType type))
                {
                    _interfaces[type.Name] = type;
                }
            }
        }

        private void CompleteIsOfType(ICompletionContext context)
        {
            if (_isOfType == null)
            {
                if (context.IsOfType != null)
                {
                    IsOfTypeFallback isOfType = context.IsOfType;
                    _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
                }
                else if (ClrType == typeof(object))
                {
                    _isOfType = IsOfTypeWithName;
                }
                else
                {
                    _isOfType = IsOfTypeWithClrType;
                }
            }
        }

        private bool ValidateFields(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            ObjectFieldDefinition[] invalidFields =
                definition.Fields.Where(t => t.Type is null).ToArray();

            foreach (ObjectFieldDefinition field in invalidFields)
            {
                // TODO : resources
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        "Unable to infer or resolve the type of " +
                        "field {0}.{1}. Try to explicitly provide the " +
                        "type like the following: " +
                        "`descriptor.Field(\"field\")" +
                        ".Type<List<StringType>>()`.",
                        Name,
                        field.Name))
                    .SetCode(ErrorCodes.Schema.NoFieldType)
                    .SetTypeSystemObject(this)
                    .SetPath(Path.New(Name).Append(field.Name))
                    .SetExtension(TypeErrorFields.Definition, field)
                    .Build());
            }

            return invalidFields.Length == 0;
        }

        private bool IsOfTypeWithClrType(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return ClrType.IsInstanceOfType(result);
        }

        private bool IsOfTypeWithName(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }

            Type type = result.GetType();
            return Name.Equals(type.Name);
        }

        private class InterfaceMap
            : IDictionary<NameString, InterfaceType>
        {
            public InterfaceType this[NameString key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ICollection<NameString> Keys => throw new NotImplementedException();

            public ICollection<InterfaceType> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(NameString key, InterfaceType value)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<NameString, InterfaceType> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<NameString, InterfaceType> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(NameString key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<NameString, InterfaceType>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<NameString, InterfaceType>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(NameString key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<NameString, InterfaceType> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(NameString key, [MaybeNullWhen(false)] out InterfaceType value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
