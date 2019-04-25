﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate
{
    public partial class Schema
    {
        private readonly Action<ISchemaTypeDescriptor> _configure;
        private bool _sealed;

        protected internal Schema()
        {
            _configure = Configure;
        }

        public Schema(Action<ISchemaTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected virtual void Configure(ISchemaTypeDescriptor descriptor)
        {
        }

        protected sealed override SchemaTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = SchemaTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());

            _configure(descriptor);

            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            SchemaTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.Reference));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            SchemaTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            var directives = new DirectiveCollection(
                this, definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;
            Services = context.Services;
        }

        internal void CompleteSchema(
            SchemaTypesDefinition schemaTypesDefinition)
        {
            if (schemaTypesDefinition == null)
            {
                throw new ArgumentNullException(nameof(schemaTypesDefinition));
            }

            if (_sealed)
            {
                throw new InvalidOperationException(
                    "This schema is already sealed and cannot be mutated.");
            }

            DirectiveTypes = schemaTypesDefinition.DirectiveTypes;
            _types = new SchemaTypes(schemaTypesDefinition);
            _directiveTypes = DirectiveTypes.ToDictionary(t => t.Name);
            _sealed = true;
        }
    }
}
