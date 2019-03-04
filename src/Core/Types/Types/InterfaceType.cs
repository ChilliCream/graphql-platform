using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : NamedTypeBase
        , IComplexOutputType
        , IHasClrType
    {
        private ResolveAbstractType _resolveAbstractType;

        protected InterfaceType()
            : base(TypeKind.Interface)
        {
            Initialize(Configure);
        }

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
            : base(TypeKind.Interface)
        {
            Initialize(configure);
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; private set; }

        public FieldCollection<InterfaceField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public Type ClrType { get; protected set; }

        public ObjectType ResolveType(
            IResolverContext context,
            object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType.Invoke(context, resolverResult);
        }

        #region Configuration

        internal virtual InterfaceTypeDescriptor CreateDescriptor() =>
            new InterfaceTypeDescriptor();

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        {

        }

        #endregion

        #region Initialization

        private void Initialize(Action<IInterfaceTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InterfaceTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            InterfaceTypeDescription description =
                descriptor.CreateDescription();

            _resolveAbstractType = description.ResolveAbstractType;

            SyntaxNode = description.SyntaxNode;
            Fields = new FieldCollection<InterfaceField>(
                description.Fields.Select(t => new InterfaceField(t)));

            Initialize(description.Name, description.Description,
                new DirectiveCollection(this,
                    DirectiveLocation.Interface,
                    description.Directives));
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.CompleteType(context);
            }

            CompleteAbstractTypeResolver(context);
            CompleteClrType(context);
            ValidateFieldsRequirement(context);
        }

        private void CompleteAbstractTypeResolver(
            ITypeInitializationContext context)
        {
            if (_resolveAbstractType == null)
            {
                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType> types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types == null)
                    {
                        types = context.GetPossibleTypes(this);
                    }

                    foreach (ObjectType type in types)
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

        private void CompleteClrType(
            ITypeInitializationContext context)
        {
            if (ClrType == null
                && context.TryGetNativeType(this, out Type clrType))
            {
                ClrType = clrType;
            }

            if (ClrType == null)
            {
                ClrType = typeof(object);
            }
        }

        private void ValidateFieldsRequirement(
            ITypeInitializationContext context)
        {
            if (Fields.Count == 0)
            {
                context.ReportError(new SchemaError(
                    $"Interface `{Name}` has no fields declared.",
                    this));
            }
        }

        #endregion
    }

    public class InterfaceType<T>
        : InterfaceType
    {
        public InterfaceType()
        {
            ClrType = typeof(T);
        }

        public InterfaceType(Action<IInterfaceTypeDescriptor<T>> configure)
            : base(d => configure((IInterfaceTypeDescriptor<T>)d))
        {
            ClrType = typeof(T);
        }

        #region Configuration

        internal sealed override InterfaceTypeDescriptor CreateDescriptor() =>
            new InterfaceTypeDescriptor<T>();

        protected sealed override void Configure(
            IInterfaceTypeDescriptor descriptor)
        {
            Configure((IInterfaceTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IInterfaceTypeDescriptor<T> descriptor)
        {

        }

        #endregion
    }
}
