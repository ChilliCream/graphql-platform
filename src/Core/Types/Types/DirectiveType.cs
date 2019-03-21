using System;
using System.Collections.Generic;
using System.Linq;
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
        , IHasName
        , IHasDescription
    {
        private readonly Action<IDirectiveTypeDescriptor> _configure;
        private ITypeConversion _converter;
        private List<DirectiveMiddleware> _components;

        protected DirectiveType()
        {
            _configure = Configure;
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public DirectiveDefinitionNode SyntaxNode { get; private set; }

        internal Type ClrType { get; private set; }

        public bool IsRepeatable { get; private set; }

        public ICollection<DirectiveLocation> Locations { get; private set; }

        public FieldCollection<Argument> Arguments { get; private set; }

        public DirectiveMiddleware Middleware { get; private set; }

        public bool IsExecutable { get; private set; }

        public FieldDelegate CompileMiddleware(
            FieldDelegate first,
            Func<IMiddlewareContext, IDirectiveContext> createContext)
        {
            FieldDelegate next = first;

            foreach (DirectiveMiddleware component in _components)
            {
                DirectiveDelegate directiveDelegate = component.Invoke(next);
                next = context => directiveDelegate(createContext(context));
            }

            return next;

        }

        #region Initialization

        protected override DirectiveTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            DirectiveTypeDescriptor descriptor = DirectiveTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IDirectiveTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            DirectiveTypeDefinition definition)
        {
            context.RegisterDependencyRange(
                definition.GetDependencies(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            DirectiveTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            _converter = context.Services.GetTypeConversion();
            _components = definition.MiddlewareComponents.ToList();
            _components.Reverse();

            SyntaxNode = definition.SyntaxNode;
            ClrType = definition.ClrType;
            IsRepeatable = definition.IsRepeatable;
            Locations = definition.Locations.ToList().AsReadOnly();
            Arguments = new FieldCollection<Argument>(
                definition.Arguments.Select(t => new Argument(t)));
            IsExecutable = _components.Any();

            if (!Locations.Any())
            {
                // TODO : resources
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(
                        $"The `{Name}` directive does not declare any " +
                        "location on which it is valid.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(definition.SyntaxNode)
                    .Build());
            }

            FieldInitHelper.CompleteFields(context, definition, Arguments);
        }

        #endregion

        internal object DeserializeArgument(
            Argument argument,
            IValueNode valueNode,
            Type targetType)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            if (valueNode == null)
            {
                throw new ArgumentNullException(nameof(valueNode));
            }

            object obj = argument.Type.ParseLiteral(valueNode);
            if (targetType.IsInstanceOfType(obj))
            {
                return obj;
            }

            if (_converter.TryConvert(typeof(object), targetType,
                obj, out object o))
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
