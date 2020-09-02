using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class DirectiveType
        : TypeSystemObjectBase<DirectiveTypeDefinition>
        , IHasRuntimeType
    {
        private readonly Action<IDirectiveTypeDescriptor> _configure;
        private ITypeConverter _converter;

        protected DirectiveType()
        {
            _configure = Configure;
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public DirectiveDefinitionNode SyntaxNode { get; private set; }

        public Type RuntimeType { get; private set; }

        public bool IsRepeatable { get; private set; }

        public ICollection<DirectiveLocation> Locations { get; private set; }

        public FieldCollection<Argument> Arguments { get; private set; }

        public IReadOnlyList<DirectiveMiddleware> MiddlewareComponents { get; private set; }

        public bool IsExecutable { get; private set; }

        protected override DirectiveTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = DirectiveTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IDirectiveTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            DirectiveTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            RuntimeType = definition.RuntimeType != GetType()
                ? definition.RuntimeType
                : typeof(object);
            IsRepeatable = definition.IsRepeatable;

            RegisterDependencies(context, definition);
        }

        private void RegisterDependencies(
           ITypeDiscoveryContext context,
           DirectiveTypeDefinition definition)
        {
            context.RegisterDependencyRange(
                definition.Arguments.Select(t => t.Type),
                TypeDependencyKind.Completed);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            DirectiveTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            _converter = context.Services.GetTypeConverter();
            MiddlewareComponents = definition.MiddlewareComponents.ToList().AsReadOnly();

            SyntaxNode = definition.SyntaxNode;
            Locations = definition.Locations.ToList().AsReadOnly();
            Arguments = new FieldCollection<Argument>(
                definition.Arguments.Select(t => new Argument(t)),
                context.DescriptorContext.Options.SortFieldsByName);
            IsExecutable = MiddlewareComponents.Count > 0;

            if (Locations.Count == 0)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.DirectiveType_NoLocations,
                        Name))
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(definition.SyntaxNode)
                    .Build());
            }

            FieldInitHelper.CompleteFields(context, definition, Arguments);
        }

        internal object DeserializeArgument(
            Argument argument,
            IValueNode valueNode,
            Type targetType)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            if (valueNode is null)
            {
                throw new ArgumentNullException(nameof(valueNode));
            }

            object obj = argument.Type.ParseLiteral(valueNode);

            if (targetType.IsInstanceOfType(obj))
            {
                return obj;
            }

            if (_converter.TryConvert(typeof(object), targetType, obj, out object o))
            {
                return o;
            }

            throw new ArgumentException(
                TypeResources.DirectiveType_UnableToConvert,
                nameof(targetType));
        }

        internal T DeserializeArgument<T>(
            Argument argument,
            IValueNode valueNode)
        {
            return (T)DeserializeArgument(argument, valueNode, typeof(T));
        }
    }
}
