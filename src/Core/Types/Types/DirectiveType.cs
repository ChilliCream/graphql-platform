﻿using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class DirectiveType
        : TypeSystemBase
    {
        private IDirectiveMiddleware _middleware;

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

        public NameString Name { get; private set; }

        public string Description { get; private set; }

        public bool IsRepeatable { get; private set; }

        public ICollection<DirectiveLocation> Locations { get; private set; }

        public FieldCollection<InputField> Arguments { get; private set; }

        public DirectiveMiddleware Middleware { get; private set; }

        public bool IsExecutable { get; private set; }

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
            IsRepeatable = description.IsRepeatable;
            Locations = description.Locations.ToList().AsReadOnly();
            Arguments = new FieldCollection<InputField>(
                description.Arguments.Select(t => new InputField(t)));
            _middleware = description.Middleware;
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            if (Locations.Count == 0)
            {
                context.ReportError(new SchemaError(
                    $"The `{Name}` directive does not declare any " +
                    "location on which it is valid."));
            }

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.RegisterDependencies(context);
            }

            if (_middleware != null)
            {
                context.RegisterMiddleware(_middleware);
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

            if (context.GetMiddleware(Name) is DirectiveDelegateMiddleware m)
            {
                Middleware = m.Middleware;
                IsExecutable = true;
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
