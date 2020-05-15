using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Types
{
    public class InputUnionType
        : NamedTypeBase<InputUnionTypeDefinition>
        , INamedInputType
    {
        private const string _typeReference = "typeReference";

        private readonly Action<IInputUnionTypeDescriptor> _configure;

        private readonly Dictionary<NameString, InputObjectType> _typeMap =
            new Dictionary<NameString, InputObjectType>();

        protected InputUnionType()
        {
            _configure = Configure;
        }

        public InputUnionType(Action<IInputUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.InputUnion;

        public InputUnionTypeDefinitionNode SyntaxNode { get; private set; }

        public IReadOnlyDictionary<NameString, InputObjectType> Types => _typeMap;

        public override bool IsAssignableFrom(INamedType namedType)
        {
            switch (namedType.Kind)
            {
                case TypeKind.InputUnion:
                    return ReferenceEquals(namedType, this);

                case TypeKind.InputObject:
                    return _typeMap.ContainsKey(((InputObjectType)namedType).Name);

                default:
                    return false;
            }
        }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode ov)
            {
                for (var i = 0; i < ov.Fields.Count; i++)
                {
                    ObjectFieldNode field = ov.Fields[i];
                    if (IntrospectionFields.TypeName.Equals(field.Name.Value))
                    {
                        if (field.Value is StringValueNode typename &&
                            _typeMap.TryGetValue(
                                typename.Value,
                                out InputObjectType type))
                        {
                            return type.ParseLiteral(literal);
                        }
                        else
                        {
                            throw new InputObjectSerializationException(
                                TypeResources.InputUnionType_UnableToResolveType);
                        }
                    }
                }

                throw new InputObjectSerializationException(
                    TypeResources.InputUnionType_TypeNameNotSpecified);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new InputObjectSerializationException(
                TypeResources.InputUnionType_CannotParseLiteral);
        }

        public bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            foreach (InputObjectType type in _typeMap.Values)
            {
                if (type.IsInstanceOfType(value))
                {
                    return true;
                }
            }

            return false;
        }

        public IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            foreach (InputObjectType type in _typeMap.Values)
            {
                if (type.IsInstanceOfType(value) &&
                    type.ParseValue(value) is ObjectValueNode valueNode &&
                    valueNode.Fields is List<ObjectFieldNode> fields)
                {
                    fields.Add(new ObjectFieldNode(IntrospectionFields.TypeName, type.Name));
                    return valueNode.WithFields(fields);
                }
            }

            throw new InputObjectSerializationException(
                TypeResources.InputUnionType_TypeNameNotSpecified);
        }

        public object Serialize(object value)
        {
            if (TrySerialize(value, out object serialized))
            {
                return serialized;
            }
            throw new InputObjectSerializationException(
                "The specified value is not a valid input object.");
        }

        public virtual bool TrySerialize(object value, out object serialized)
        {
            try
            {
                if (value is null)
                {
                    serialized = null;
                    return true;
                }

                //TODO: Do we have to extend with __typename?
                if (value is IReadOnlyDictionary<string, object>
                    || value is IDictionary<string, object>)
                {
                    serialized = value;
                    return true;
                }

                foreach (InputObjectType type in _typeMap.Values)
                {
                    if (type.IsInstanceOfType(value) &&
                        type.TrySerialize(value, out var serializedDict) &&
                        serializedDict is Dictionary<string, object> serializedTyped)
                    {
                        serializedTyped.Add(IntrospectionFields.TypeName, type.Name);
                        serialized = serializedTyped;
                        return true;
                    }
                }
                serialized = null;
                return false;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }

        public object Deserialize(object serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            if (serialized is IReadOnlyDictionary<string, object> dict)
            {
                return Deserialize(dict);
            }

            if (ClrType != typeof(object) && ClrType.IsInstanceOfType(serialized))
            {
                return serialized;
            }

            throw new InputObjectSerializationException(
                "The specified value is not a serialized input object.");
        }

        private object Deserialize(IReadOnlyDictionary<string, object> dict)
        {
            if (dict.TryGetValue(IntrospectionFields.TypeName, out object value) &&
                value is string typename)
            {
                if (_typeMap.TryGetValue(typename, out InputObjectType type))
                {
                    return type.Deserialize(dict);
                }
                else
                {
                    throw new InputObjectSerializationException(
                        TypeResources.InputUnionType_UnableToResolveType);
                }
            }
            throw new InputObjectSerializationException(
                TypeResources.InputUnionType_TypeNameNotSpecified);
        }

        public virtual bool TryDeserialize(object serialized, out object value)
        {
            try
            {
                if (serialized is null)
                {
                    value = null;
                    return true;
                }

                if (serialized is IReadOnlyDictionary<string, object> dict)
                {
                    value = Deserialize(dict);
                    return true;
                }

                if (ClrType != typeof(object) && ClrType.IsInstanceOfType(serialized))
                {
                    value = serialized;
                    return true;
                }

                value = null;
                return false;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        protected override InputUnionTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = InputUnionTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInputUnionTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputUnionTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.Types,
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            SetTypeIdentity(typeof(InputUnionType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputUnionTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);
            CompleteTypeSet(context, definition);
        }

        private void CompleteTypeSet(
            ICompletionContext context,
            InputUnionTypeDefinition definition)
        {
            var typeSet = new HashSet<InputObjectType>();

            OnCompleteTypeSet(context, definition, typeSet);

            foreach (InputObjectType inputObjectType in typeSet)
            {
                _typeMap[inputObjectType.Name] = inputObjectType;
            }

            if (typeSet.Count == 0)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(TypeResources.InputUnionType_MustHaveTypes)
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(this)
                    .AddSyntaxNode(SyntaxNode)
                    .Build());
            }
        }

        protected virtual void OnCompleteTypeSet(
            ICompletionContext context,
            InputUnionTypeDefinition definition,
            ISet<InputObjectType> typeSet)
        {
            foreach (ITypeReference typeReference in definition.Types)
            {
                if (context.TryGetType(typeReference, out InputObjectType ot))
                {
                    typeSet.Add(ot);
                }
                else
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.InputUnionType_UnableToResolveType)
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(this)
                        .SetExtension(_typeReference, typeReference)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
                }
            }
        }
    }
}
