using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionType
        : NamedTypeBase<UnionTypeDefinition>
        , INamedOutputType
    {
        private const string _typeReference = "typeReference";

        private readonly Action<IUnionTypeDescriptor> _configure;
        private readonly Dictionary<NameString, ObjectType> _typeMap =
            new Dictionary<NameString, ObjectType>();
        private ResolveAbstractType _resolveAbstractType;

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

        public UnionTypeDefinitionNode SyntaxNode { get; private set; }

        public IReadOnlyDictionary<NameString, ObjectType> Types => _typeMap;

        public ObjectType ResolveType(
            IResolverContext context, object resolverResult) =>
            _resolveAbstractType(context, resolverResult);

        #region Initialization

        protected override UnionTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = UnionTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            UnionTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.Types,
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            SetTypeIdentity(typeof(UnionType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            UnionTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            CompleteTypeSet(context, definition);
            CompleteResolveAbstractType(definition.ResolveAbstractType);
        }

        private void CompleteTypeSet(
            ICompletionContext context,
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
            ICompletionContext context,
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
            if (resolveAbstractType == null)
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

        #endregion
    }
}
