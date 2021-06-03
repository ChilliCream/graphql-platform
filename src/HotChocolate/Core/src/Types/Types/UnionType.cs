using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class UnionType
        : NamedTypeBase<UnionTypeDefinition>
        , IUnionType
    {
        private const string _typeReference = "typeReference";

        private readonly Dictionary<NameString, ObjectType> _typeMap = new();

        private Action<IUnionTypeDescriptor>? _configure;
        private ResolveAbstractType? _resolveAbstractType;

        protected UnionType()
        {
            _configure = Configure;
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Union;

        public UnionTypeDefinitionNode? SyntaxNode { get; private set; }

        public IReadOnlyDictionary<NameString, ObjectType> Types => _typeMap;

        IReadOnlyCollection<IObjectType> IUnionType.Types => _typeMap.Values;

        public override bool IsAssignableFrom(INamedType namedType)
        {
            switch (namedType.Kind)
            {
                case TypeKind.Union:
                    return ReferenceEquals(namedType, this);

                case TypeKind.Object:
                    return _typeMap.ContainsKey(((ObjectType)namedType).Name);

                default:
                    return false;
            }
        }

        public bool ContainsType(ObjectType objectType)
        {
            if (objectType is null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return _typeMap.ContainsKey(objectType.Name);
        }


        bool IUnionType.ContainsType(IObjectType objectType)
        {
            if (objectType is null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return _typeMap.ContainsKey(objectType.Name);
        }

        public bool ContainsType(NameString typeName) =>
            _typeMap.ContainsKey(typeName.EnsureNotEmpty(nameof(typeName)));

        public ObjectType? ResolveConcreteType(
            IResolverContext context,
            object resolverResult) =>
            _resolveAbstractType?.Invoke(context, resolverResult);

        IObjectType? IUnionType.ResolveConcreteType(
            IResolverContext context,
            object resolverResult) =>
            ResolveConcreteType(context, resolverResult);

        protected override UnionTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                UnionTypeDescriptor.FromSchemaType(context.DescriptorContext, GetType());

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            UnionTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.Types,
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.GetDirectives().Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            SetTypeIdentity(typeof(UnionType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            UnionTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            CompleteTypeSet(context, definition);
            CompleteResolveAbstractType(definition.ResolveAbstractType);
        }

        private void CompleteTypeSet(
            ITypeCompletionContext context,
            UnionTypeDefinition definition)
        {
            var typeSet = new HashSet<ObjectType>();

            OnCompleteTypeSet(context, definition, typeSet);

            foreach (ObjectType objectType in typeSet)
            {
                _typeMap[objectType.Name] = objectType;
            }

            if (typeSet.Count == 0)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(TypeResources.UnionType_MustHaveTypes)
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(this)
                    .AddSyntaxNode(SyntaxNode)
                    .Build());
            }
        }

        protected virtual void OnCompleteTypeSet(
            ITypeCompletionContext context,
            UnionTypeDefinition definition,
            ISet<ObjectType> typeSet)
        {
            foreach (ITypeReference typeReference in definition.Types)
            {
                if (context.TryGetType(typeReference, out ObjectType ot))
                {
                    typeSet.Add(ot);
                }
                else
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.UnionType_UnableToResolveType)
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(this)
                        .SetExtension(_typeReference, typeReference)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
                }
            }
        }

        private void CompleteResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            if (resolveAbstractType is null)
            {
                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                _resolveAbstractType = (c, r) =>
                {
                    foreach (ObjectType type in _typeMap.Values)
                    {
                        if (type.IsOfType(c, r))
                        {
                            return type;
                        }
                    }
                    return null;
                };
            }
            else
            {
                _resolveAbstractType = resolveAbstractType;
            }
        }
    }
}
