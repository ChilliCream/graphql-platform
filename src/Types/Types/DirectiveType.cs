using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class DirectiveType
        : TypeSystemBase
    {
        protected DirectiveType()
        {
            Initialize(Configure);
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        internal Type ClrType { get; private set; }

        public DirectiveDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public ICollection<DirectiveLocation> Locations { get; private set; }

        public FieldCollection<InputField> Arguments { get; private set; }

        #region Configuration

        internal virtual DirectiveTypeDescriptor CreateDescriptor() =>
            new DirectiveTypeDescriptor();

        protected virtual void Configure(IDirectiveTypeDescriptor descriptor)
        {
        }

        #endregion

        #region  Initialization

        private void Initialize(Action<IDirectiveTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            DirectiveTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            DirectiveTypeDescription description =
                descriptor.CreateDescription();

            ClrType = description.ClrType;
            SyntaxNode = description.SyntaxNode;
            Name = description.Name;
            Description = description.Description;
            Locations = description.Locations.ToList().AsReadOnly();
            Arguments = new FieldCollection<InputField>(
                description.Arguments.Select(t => new InputField(t)));
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.CompleteType(context);
            }
        }

        #endregion
    }

    public class DirectiveType<TDirective>
        : DirectiveType
    {
        protected DirectiveType()
        {
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
            : base(configure)
        {
        }

        #region Configuration

        internal sealed override DirectiveTypeDescriptor CreateDescriptor() =>
            new DirectiveTypeDescriptor<TDirective>();

        protected sealed override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            Configure((IDirectiveTypeDescriptor<TDirective>)descriptor);
        }

        protected virtual void Configure(
            IDirectiveTypeDescriptor<TDirective> descriptor)
        {

        }

        #endregion
    }
}
