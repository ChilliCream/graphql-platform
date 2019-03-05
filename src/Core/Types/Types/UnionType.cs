using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class UnionType
        : NamedTypeBase
        , INamedOutputType
    {
        private readonly Dictionary<NameString, ObjectType> _typeMap =
            new Dictionary<NameString, ObjectType>();
        private List<TypeReference> _types;
        private ResolveAbstractType _resolveAbstractType;

        protected UnionType()
            : base(TypeKind.Union)
        {
            Initialize(Configure);
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
            : base(TypeKind.Union)
        {
            Initialize(configure);
        }

        public UnionTypeDefinitionNode SyntaxNode { get; private set; }

        public IReadOnlyDictionary<NameString, ObjectType> Types => _typeMap;

        public ObjectType ResolveType(
            IResolverContext context, object resolverResult)
            => _resolveAbstractType(context, resolverResult);

        #region Configuration

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        #endregion

        #region Initialization

        internal virtual UnionTypeDescriptor CreateDescriptor() =>
            new UnionTypeDescriptor(GetType());

        private void Initialize(Action<IUnionTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new UnionTypeDescriptor(GetType());
            configure(descriptor);

            UnionTypeDescription description = descriptor.CreateDescription();

            _types = description.Types;
            _resolveAbstractType = description.ResolveAbstractType;

            Initialize(description.Name, description.Description,
                new DirectiveCollection(this,
                    DirectiveLocation.Union,
                    description.Directives));
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (TypeReference typeReference in _types)
            {
                context.RegisterType(typeReference);
            }
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

        protected virtual ISet<ObjectType> CreateUnionTypeSet(
            ITypeInitializationContext context)
        {
            return new HashSet<ObjectType>(_types
                .Select(t => context.GetType<ObjectType>(t))
                .Where(t => t != null));
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
