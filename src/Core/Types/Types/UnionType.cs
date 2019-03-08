using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionType
        : NamedTypeBase<UnionTypeDefinition>
        , INamedOutputType
    {
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
            IResolverContext context, object resolverResult)
            => _resolveAbstractType(context, resolverResult);

        #region Initialization

        protected override UnionTypeDefinition CreateDefinition(IInitializationContext context)
        {
            UnionTypeDescriptor descriptor = UnionTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
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
        }

        protected override void OnCompleteObject(
            ICompletionContext context,
            UnionTypeDefinition definition)
        {
            base.OnCompleteObject(context, definition);

            Description = definition.Description;
            definition.
        }

        private void Initialize(Action<IUnionTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new UnionTypeDescriptor(GetType());
            configure(descriptor);

            UnionTypeDescription description = descriptor.CreateDescription();

            _resolveAbstractType = description.ResolveAbstractType;

            Initialize(description.Name, description.Description,
                new DirectiveCollection(this,
                    DirectiveLocation.Union,
                    description.Directives));
        }



        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            CompleteTypes(context);
            CompleteResolveAbstractType();
        }

        private void CompleteTypes(
            ITypeInitializationContext context)
        {
            if (_types != null)
            {
                foreach (ObjectType memberType in CreateUnionTypeSet(context))
                {
                    _typeMap[memberType.Name] = memberType;
                }
            }

            if (_typeMap.Count == 0)
            {
                context.ReportError(new SchemaError(
                    "A Union type must define one or more unique member types.",
                    this));
            }
        }

        protected virtual ISet<ObjectType> OnCompleteTypeSet(
            ICompletionContext context,
            UnionTypeDefinition definition)
        {
            var typeSet = new HashSet<ObjectType>();

            foreach (ITypeReference typeReference in definition.Types)
            {
                if (context.TryGetType(typeReference, out ObjectType ot))
                {
                    typeSet.Add(ot);
                }
                else
                {
                    // TODO : RESOURCES
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage("")
                        .SetCode(TypeErrorCodes.MissingType)
                        .SetTypeSystemObject(this)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
                }
            }

            return typeSet;
        }

        private void CompleteResolveAbstractType()
        {
            if (_resolveAbstractType == null)
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
        }



        #endregion
    }

    public class UnionType<T>
        : UnionType
    {
        public UnionType()
        {
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
            : base(configure)
        {
        }

        internal override UnionTypeDescriptor CreateDescriptor() =>
            new UnionTypeDescriptor(typeof(T));

        protected override ISet<ObjectType> CreateUnionTypeSet(
            ITypeInitializationContext context)
        {
            ISet<ObjectType> typeSet = base.CreateUnionTypeSet(context);

            Type markerType = typeof(T);

            foreach (IType type in context.GetTypes())
            {
                if (type is ObjectType objectType
                    && markerType.IsAssignableFrom(objectType.ClrType))
                {
                    typeSet.Add(objectType);
                }
            }

            return typeSet;
        }
    }
}
