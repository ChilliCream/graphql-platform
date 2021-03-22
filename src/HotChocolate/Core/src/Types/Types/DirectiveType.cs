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

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// A GraphQL schema describes directives which are used to annotate various parts of a
    /// GraphQL document as an indicator that they should be evaluated differently by a
    /// validator, executor, or client tool such as a code generator.
    ///
    /// http://spec.graphql.org/draft/#sec-Type-System.Directives
    /// </summary>
    public class DirectiveType
        : TypeSystemObjectBase<DirectiveTypeDefinition>
        , IHasRuntimeType
    {
        // see: http://spec.graphql.org/draft/#ExecutableDirectiveLocation
        private static readonly HashSet<DirectiveLocation> _executableLocations =
            new()
            {
                DirectiveLocation.Query,
                DirectiveLocation.Mutation,
                DirectiveLocation.Subscription,
                DirectiveLocation.Field,
                DirectiveLocation.FragmentDefinition,
                DirectiveLocation.FragmentSpread,
                DirectiveLocation.InlineFragment,
                DirectiveLocation.VariableDefinition
            };

        // see: http://spec.graphql.org/draft/#TypeSystemDirectiveLocation
        private static readonly HashSet<DirectiveLocation> _typeSystemLocations =
            new()
            {
                DirectiveLocation.Schema,
                DirectiveLocation.Scalar,
                DirectiveLocation.Object,
                DirectiveLocation.FieldDefinition,
                DirectiveLocation.ArgumentDefinition,
                DirectiveLocation.Interface,
                DirectiveLocation.Union,
                DirectiveLocation.Enum,
                DirectiveLocation.EnumValue,
                DirectiveLocation.InputObject,
                DirectiveLocation.InputFieldDefinition
            };

        private Action<IDirectiveTypeDescriptor>? _configure;
        private ITypeConverter _converter = default!;

        protected DirectiveType()
        {
            _configure = Configure;
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public DirectiveDefinitionNode? SyntaxNode { get; private set; }

        public Type RuntimeType { get; private set; } = default!;

        public bool IsRepeatable { get; private set; }

        public ICollection<DirectiveLocation> Locations { get; private set; } = default!;

        public FieldCollection<Argument> Arguments { get; private set; }  = default!;

        public IReadOnlyList<DirectiveMiddleware> MiddlewareComponents { get; private set; } =
            default!;

        public bool HasMiddleware { get; private set; }

        /// <summary>
        /// Defines that this directive can be used in executable GraphQL documents.
        ///
        /// In order to be executable a directive must at least be valid
        /// in one of the following locations:
        /// QUERY (<see cref="DirectiveLocation.Query"/>)
        /// MUTATION (<see cref="DirectiveLocation.Mutation"/>)
        /// SUBSCRIPTION (<see cref="DirectiveLocation.Subscription"/>)
        /// FIELD (<see cref="DirectiveLocation.Field"/>)
        /// FRAGMENT_DEFINITION (<see cref="DirectiveLocation.FragmentDefinition"/>)
        /// FRAGMENT_SPREAD (<see cref="DirectiveLocation.FragmentSpread"/>)
        /// INLINE_FRAGMENT (<see cref="DirectiveLocation.InlineFragment"/>)
        /// VARIABLE_DEFINITION (<see cref="DirectiveLocation.VariableDefinition"/>)
        /// </summary>
        public bool IsExecutableDirective { get; private set; }

        /// <summary>
        /// Defines that this directive can be applied to type system members.
        ///
        /// In order to be a type system directive it must at least be valid
        /// in one of the following locations:
        /// SCHEMA (<see cref="DirectiveLocation.Schema"/>)
        /// SCALAR (<see cref="DirectiveLocation.Scalar"/>)
        /// OBJECT (<see cref="DirectiveLocation.Object"/>)
        /// FIELD_DEFINITION (<see cref="DirectiveLocation.FieldDefinition"/>)
        /// ARGUMENT_DEFINITION (<see cref="DirectiveLocation.ArgumentDefinition"/>)
        /// INTERFACE (<see cref="DirectiveLocation.Interface"/>)
        /// UNION (<see cref="DirectiveLocation.Union"/>)
        /// ENUM (<see cref="DirectiveLocation.Enum"/>)
        /// ENUM_VALUE (<see cref="DirectiveLocation.EnumValue"/>)
        /// INPUT_OBJECT (<see cref="DirectiveLocation.InputObject"/>)
        /// INPUT_FIELD_DEFINITION (<see cref="DirectiveLocation.InputFieldDefinition"/>)
        /// </summary>
        public bool IsTypeSystemDirective { get; private set; }

        internal bool IsPublic { get; private set; }

        protected override DirectiveTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                DirectiveTypeDescriptor.FromSchemaType( context.DescriptorContext, GetType());

            _configure!(descriptor);
            _configure = null;

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
                definition.GetArguments().Select(t => t.Type),
                TypeDependencyKind.Completed);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            DirectiveTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            _converter = context.Services.GetTypeConverter();
            MiddlewareComponents = definition.GetMiddlewareComponents();

            SyntaxNode = definition.SyntaxNode;
            Locations = definition.GetLocations().ToList().AsReadOnly();
            Arguments = FieldCollection<Argument>.From(
                definition
                    .GetArguments()
                    .Select(t => new Argument(t, new FieldCoordinate(Name, t.Name))),
                context.DescriptorContext.Options.SortFieldsByName);
            HasMiddleware = MiddlewareComponents.Count > 0;
            IsPublic = definition.IsPublic;

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

            IsExecutableDirective = _executableLocations.Overlaps(Locations);
            IsTypeSystemDirective = _typeSystemLocations.Overlaps(Locations);

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
