using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : TypeBase
        , IComplexOutputType
    {
        private ResolveAbstractType _resolveAbstractType;

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
            : this(ExecuteConfigure(configure))
        {
        }

        internal InterfaceType(Func<InterfaceTypeDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal InterfaceType(InterfaceTypeDescription description)
            : base(TypeKind.InputObject)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (string.IsNullOrEmpty(description.Name))
            {
                throw new ArgumentException(
                    "The name of named types mustn't be null or empty.");
            }

            _resolveAbstractType = description.ResolveAbstractType;
            SyntaxNode = description.SyntaxNode;
            Name = description.Name;
            Description = description.Description;
            Fields = new FieldCollection<InterfaceField>(
                description.Fields.Select(t => new InterfaceField(t)));
        }

        private static InterfaceTypeDescription ExecuteConfigure(
            Action<IInterfaceTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new InterfaceTypeDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public FieldCollection<InterfaceField> Fields { get; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType.Invoke(context, resolverResult);
        }

        #region Configuration

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

        #endregion

        #region Initialization


        protected override void OnRegisterDependencies(ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.CompleteType(context);
            }

            CompleteAbstractTypeResolver(context);
        }


        private void CompleteAbstractTypeResolver(
            ITypeInitializationContext context)
        {
            if (_resolveAbstractType == null)
            {
                // if there is now custom type resolver we will use this default
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

                    return null; // todo: should we throw instead?
                };
            }
        }

        #endregion
    }
}
