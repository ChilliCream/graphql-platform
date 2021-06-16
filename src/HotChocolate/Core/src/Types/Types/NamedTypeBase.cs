using System.Linq;
using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// A base class for named GraphQL types.
    /// </summary>
    /// <typeparam name="TDefinition">
    /// The type definition of the named GraphQL type.
    /// </typeparam>
    public abstract class NamedTypeBase<TDefinition>
        : TypeSystemObjectBase<TDefinition>
        , INamedType
        , IHasDirectives
        , IHasRuntimeType
        , IHasTypeIdentity
        where TDefinition : DefinitionBase, IHasDirectiveDefinition, IHasSyntaxNode
    {
        private IDirectiveCollection? _directives;
        private Type? _clrType;
        private ISyntaxNode? _syntaxNode;

        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        ISyntaxNode? IHasSyntaxNode.SyntaxNode => _syntaxNode;

        /// <inheritdoc />
        public abstract TypeKind Kind { get; }

        /// <inheritdoc />
        public IDirectiveCollection Directives
        {
            get
            {
                if (_directives is null)
                {
                    throw new TypeInitializationException();
                }
                return _directives;
            }
        }

        /// <inheritdoc />
        public Type RuntimeType
        {
            get
            {
                if (_clrType is null)
                {
                    throw new TypeInitializationException();
                }
                return _clrType;
            }
        }

        /// <summary>
        /// A type representing the identity of the specified type.
        /// </summary>
        public Type? TypeIdentity { get; private set; }

        /// <inheritdoc />
        public virtual bool IsAssignableFrom(INamedType type) =>
            ReferenceEquals(type, this);

        /// <inheritdoc />
        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            TDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            _clrType = definition is IHasRuntimeType clr && clr.RuntimeType != GetType()
                ? clr.RuntimeType
                : typeof(object);

            context.RegisterDependencyRange(
                definition.GetDirectives().Select(t => t.Reference));
        }

        /// <inheritdoc />
        protected override void OnCompleteType(
            ITypeCompletionContext context,
            TDefinition definition)
        {
            base.OnCompleteType(context, definition);

            _syntaxNode = definition.SyntaxNode;

            _directives =
                DirectiveCollection.CreateAndComplete(context,this, definition.GetDirectives());
        }

        /// <summary>
        /// This method allows the concrete type implementation to set its type identity.
        /// </summary>
        /// <param name="typeDefinitionOrIdentity">
        /// The type definition or type identity.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeDefinitionOrIdentity"/> is <c>null</c>.
        /// </exception>
        protected void SetTypeIdentity(Type typeDefinitionOrIdentity)
        {
            if (typeDefinitionOrIdentity is null)
            {
                throw new ArgumentNullException(nameof(typeDefinitionOrIdentity));
            }

            if (!typeDefinitionOrIdentity.IsGenericTypeDefinition)
            {
                TypeIdentity = typeDefinitionOrIdentity;
            }
            else if (RuntimeType != typeof(object))
            {
                TypeIdentity = typeDefinitionOrIdentity.MakeGenericType(RuntimeType);
            }
        }
    }
}
