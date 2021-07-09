using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.FieldInitHelper;
using static HotChocolate.Types.CompleteInterfacesHelper;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL operations are hierarchical and composed, describing a tree of information.
    /// While Scalar types describe the leaf values of these hierarchical operations,
    /// Objects describe the intermediate levels.
    ///
    /// GraphQL Objects represent a list of named fields, each of which yield a value of a
    /// specific type. Object values should be serialized as ordered maps, where the selected
    /// field names (or aliases) are the keys and the result of evaluating the field is the value,
    /// ordered by the order in which they appear in the selection set.
    ///
    /// All fields defined within an Object type must not have a name which begins
    /// with "__" (two underscores), as this is used exclusively by
    /// GraphQL’s introspection system.
    /// </summary>
    public class ObjectType
        : NamedTypeBase<ObjectTypeDefinition>
        , IObjectType
    {
        private InterfaceType[] _implements = Array.Empty<InterfaceType>();
        private Action<IObjectTypeDescriptor>? _configure;
        private IsOfType? _isOfType;

        /// <summary>
        /// Initializes a new  instance of <see cref="ObjectType"/>.
        /// </summary>
        protected ObjectType()
        {
            _configure = Configure;
            Fields = FieldCollection<ObjectField>.Empty;
        }

        /// <summary>
        /// Initializes a new  instance of <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="configure">
        /// A delegate to specify the properties of this type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <c>null</c>.
        /// </exception>
        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
            Fields = FieldCollection<ObjectField>.Empty;
        }

        /// <summary>
        /// Create a object type from a type definition.
        /// </summary>
        /// <param name="definition">
        /// The object type definition that specifies the properties of the
        /// newly created object type.
        /// </param>
        /// <returns>
        /// Returns the newly created object type.
        /// </returns>
        public static ObjectType CreateUnsafe(ObjectTypeDefinition definition)
            => new() { Definition = definition };

        /// <inheritdoc />
        public override TypeKind Kind => TypeKind.Object;

        /// <inheritdoc />
        public ObjectTypeDefinitionNode? SyntaxNode { get; private set; }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        /// <summary>
        /// Gets the interfaces that are implemented by this type.
        /// </summary>
        public IReadOnlyList<InterfaceType> Implements => _implements;

        IReadOnlyList<IInterfaceType> IComplexOutputType.Implements => Implements;

        /// <summary>
        /// Gets the field that this type exposes.
        /// </summary>
        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IObjectField> IObjectType.Fields => Fields;

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        /// <inheritdoc />
        public bool IsInstanceOfType(IResolverContext context, object resolverResult)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (resolverResult is null)
            {
                throw new ArgumentNullException(nameof(resolverResult));
            }

            return _isOfType!.Invoke(context, resolverResult);
        }

        /// <summary>
        /// Specifies if the specified <paramref name="resolverResult" /> is an instance of
        /// this object type.
        /// </summary>
        /// <param name="context">
        /// The resolver context.
        /// </param>
        /// <param name="resolverResult">
        /// The result that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <see cref="resolverResult"/> is an instance of this type;
        /// otherwise, <c>false</c>.
        /// </returns>
        [Obsolete("Use IsInstanceOfType")]
        public bool IsOfType(IResolverContext context, object resolverResult)
            => IsInstanceOfType(context, resolverResult);

        /// <inheritdoc />
        public bool IsImplementing(NameString interfaceTypeName)
            => _implements.Any(t => t.Name.Equals(interfaceTypeName));

        /// <summary>
        /// Defines if this type is implementing the
        /// the given <paramref name="interfaceType" />.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface type.
        /// </param>
        public bool IsImplementing(InterfaceType interfaceType)
            => Array.IndexOf(_implements, interfaceType) != -1;

        /// <inheritdoc />
        public bool IsImplementing(IInterfaceType interfaceType)
            => interfaceType is InterfaceType i && _implements.Contains(i);

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            try
            {
                if (Definition is null)
                {
                    var descriptor = ObjectTypeDescriptor.FromSchemaType(
                        context.DescriptorContext,
                        GetType());
                    _configure!.Invoke(descriptor);
                    return descriptor.CreateDefinition();
                }

                return Definition;
            }
            finally
            {
                _configure = null;
            }
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(ObjectType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (ValidateFields(context, definition))
            {
                _isOfType = definition.IsOfType;
                SyntaxNode = definition.SyntaxNode;

                // create fields with the specified sorting settings ...
                var sortByName = context.DescriptorContext.Options.SortFieldsByName;
                var fields = definition.Fields.Where(t => !t.Ignore).Select(
                    t => new ObjectField(
                        t,
                        new FieldCoordinate(Name, t.Name),
                        sortByName)).ToList();
                Fields = FieldCollection<ObjectField>.From(fields, sortByName);

                // resolve interface references
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

                // complete the type resolver and fields
                CompleteTypeResolver(context);
                CompleteFields(context, definition, Fields);
            }
        }

        private void CompleteTypeResolver(ITypeCompletionContext context)
        {
            if (_isOfType is null)
            {
                if (context.IsOfType != null)
                {
                    IsOfTypeFallback isOfType = context.IsOfType;
                    _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
                }
                else if (RuntimeType == typeof(object))
                {
                    _isOfType = IsOfTypeWithName;
                }
                else
                {
                    _isOfType = IsOfTypeWithRuntimeType;
                }
            }
        }

        private bool ValidateFields(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            var hasErrors = false;

            foreach (ObjectFieldDefinition field in definition.Fields.Where(t => t.Type is null))
            {
                hasErrors = true;
                context.ReportError(ObjectType_UnableToInferOrResolveType(Name, this, field));
            }

            return !hasErrors;
        }

        private bool IsOfTypeWithRuntimeType(
            IResolverContext context,
            object? result) =>
            result is null || RuntimeType == result.GetType();

        private bool IsOfTypeWithName(
            IResolverContext context,
            object? result)
        {
            if (result is null)
            {
                return true;
            }

            Type type = result.GetType();
            return Name.Equals(type.Name);
        }
    }
}
