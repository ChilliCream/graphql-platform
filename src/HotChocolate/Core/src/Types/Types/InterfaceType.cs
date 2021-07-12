using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.CompleteInterfacesHelper;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL interfaces represent a list of named fields and their arguments.
    /// GraphQL objects and interfaces can then implement these interfaces
    /// which requires that the implementing type will define all fields defined by those
    /// interfaces.
    ///
    /// Fields on a GraphQL interface have the same rules as fields on a GraphQL object;
    /// their type can be Scalar, Object, Enum, Interface, or Union, or any wrapping type
    /// whose base type is one of those five.
    ///
    /// For example, an interface NamedEntity may describe a required field and types such
    /// as Person or Business may then implement this interface to guarantee this field will
    /// always exist.
    ///
    /// Types may also implement multiple interfaces. For example, Business implements both
    /// the NamedEntity and ValuedEntity interfaces in the example below.
    ///
    /// <code>
    /// interface NamedEntity {
    ///   name: String
    /// }
    ///
    /// interface ValuedEntity {
    ///   value: Int
    /// }
    ///
    /// type Person implements NamedEntity {
    ///   name: String
    ///   age: Int
    /// }
    ///
    /// type Business implements NamedEntity & ValuedEntity {
    ///   name: String
    ///   value: Int
    ///   employeeCount: Int
    /// }
    /// </code>
    /// </summary>
    public class InterfaceType
        : NamedTypeBase<InterfaceTypeDefinition>
        , IInterfaceType
    {
        private InterfaceType[] _implements = Array.Empty<InterfaceType>();
        private Action<IInterfaceTypeDescriptor>? _configure;
        private ResolveAbstractType? _resolveAbstractType;

        /// <summary>
        /// Initializes a new  instance of <see cref="InterfaceType"/>.
        /// </summary>
        protected InterfaceType()
        {
            _configure = Configure;
            Fields = FieldCollection<InterfaceField>.Empty;
        }

        /// <summary>
        /// Initializes a new  instance of <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="configure">
        /// A delegate to specify the properties of this type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <c>null</c>.
        /// </exception>
        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
            Fields = FieldCollection<InterfaceField>.Empty;
        }

        /// <summary>
        /// Create an interface type from a type definition.
        /// </summary>
        /// <param name="definition">
        /// The interface type definition that specifies the properties of the
        /// newly created interface type.
        /// </param>
        /// <returns>
        /// Returns the newly created interface type.
        /// </returns>
        public static InterfaceType CreateUnsafe(InterfaceTypeDefinition definition)
            => new() { Definition = definition };

        /// <inheritdoc />
        public override TypeKind Kind => TypeKind.Interface;

        /// <inheritdoc />
        public InterfaceTypeDefinitionNode? SyntaxNode { get; private set; }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        /// <summary>
        /// Gets the interfaces that are implemented by this type.
        /// </summary>
        public IReadOnlyList<InterfaceType> Implements => _implements;

        IReadOnlyList<IInterfaceType> IComplexOutputType.Implements => _implements;

        /// <summary>
        /// Gets the field that this type exposes.
        /// </summary>
        public FieldCollection<InterfaceField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        /// <summary>
        /// Defines if this type is implementing an interface
        /// with the given <paramref name="typeName" />.
        /// </summary>
        /// <param name="typeName">
        /// The interface type name.
        /// </param>
        public bool IsImplementing(NameString typeName)
            => _implements.Any(t => t.Name.Equals(typeName));

        /// <summary>
        /// Defines if this type is implementing the
        /// the given <paramref name="interfaceType" />.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface type.
        /// </param>
        public bool IsImplementing(InterfaceType interfaceType)
            => Array.IndexOf(_implements, interfaceType) != -1;

        /// <summary>
        /// Defines if this type is implementing the
        /// the given <paramref name="interfaceType" />.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface type.
        /// </param>
        public bool IsImplementing(IInterfaceType interfaceType)
            => interfaceType is InterfaceType i &&
               Array.IndexOf(_implements, i) != -1;

        /// <inheritdoc />
        public override bool IsAssignableFrom(INamedType namedType)
        {
            switch (namedType.Kind)
            {
                case TypeKind.Interface:
                    return ReferenceEquals(namedType, this) ||
                        ((InterfaceType)namedType).IsImplementing(this);

                case TypeKind.Object:
                    return ((ObjectType)namedType).IsImplementing(this);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Resolves the concrete type for the value of a type
        /// that implements this interface.
        /// </summary>
        /// <param name="context">
        /// The resolver context.
        /// </param>
        /// <param name="resolverResult">
        /// The value for which the type shall be resolved.
        /// </param>
        /// <returns>
        /// Returns <c>null</c> if the value is not of a type
        /// implementing this interface.
        /// </returns>
        public ObjectType? ResolveConcreteType(
            IResolverContext context,
            object resolverResult)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType!.Invoke(context, resolverResult);
        }

        IObjectType? IInterfaceType.ResolveConcreteType(
            IResolverContext context,
            object resolverResult) =>
            ResolveConcreteType(context, resolverResult);

        protected override InterfaceTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            try
            {
                if (Definition is null)
                {
                    var descriptor = InterfaceTypeDescriptor.FromSchemaType(
                        context.DescriptorContext,
                        GetType());
                    _configure!(descriptor);
                    return descriptor.CreateDefinition();
                }

                return Definition;
            }
            finally
            {
                _configure = null;
            }
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(InterfaceType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;
            var sortFieldsByName = context.DescriptorContext.Options.SortFieldsByName;

            Fields = FieldCollection<InterfaceField>.From(
                definition.Fields.Where(t => !t.Ignore).Select(
                    t => new InterfaceField(
                        t,
                        new FieldCoordinate(Name, t.Name),
                        sortFieldsByName)),
                sortFieldsByName);

            CompleteAbstractTypeResolver(context, definition.ResolveAbstractType);

            IReadOnlyList<ITypeReference> interfaces = definition.GetInterfaces();

            if (interfaces.Count > 0)
            {
                var implements = new List<InterfaceType>();

                CompleteInterfaces(
                    context,
                    interfaces,
                    implements,
                    this,
                    SyntaxNode);

                _implements = implements.ToArray();
            }

            FieldInitHelper.CompleteFields(context, definition, Fields);
        }

        private void CompleteAbstractTypeResolver(
            ITypeCompletionContext context,
            ResolveAbstractType? resolveAbstractType)
        {
            if (resolveAbstractType is null)
            {
                Func<ISchema> schemaResolver = context.GetSchemaResolver();

                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType>? types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types is null)
                    {
                        ISchema schema = schemaResolver.Invoke();
                        types = schema.GetPossibleTypes(this);
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
            else
            {
                _resolveAbstractType = resolveAbstractType;
            }
        }
    }
}
