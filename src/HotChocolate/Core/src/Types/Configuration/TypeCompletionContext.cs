using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Configuration
{
    internal sealed class TypeCompletionContext : ITypeCompletionContext
    {
        private readonly TypeDiscoveryContext _initializationContext;
        private readonly TypeReferenceResolver _typeReferenceResolver;
        private readonly Func<ISchema> _schemaResolver;

        public TypeCompletionContext(
            TypeDiscoveryContext initializationContext,
            TypeReferenceResolver typeReferenceResolver,
            IList<FieldMiddleware> globalComponents,
            IsOfTypeFallback isOfType,
            Func<ISchema> schemaResolver)
        {
            _initializationContext = initializationContext ??
                throw new ArgumentNullException(nameof(initializationContext));
            _typeReferenceResolver = typeReferenceResolver ??
                throw new ArgumentNullException(nameof(typeReferenceResolver));
            IsOfType = isOfType;
            _schemaResolver = schemaResolver ??
                throw new ArgumentNullException(nameof(schemaResolver));
            GlobalComponents = new ReadOnlyCollection<FieldMiddleware>(globalComponents);
        }

        public TypeStatus Status { get; set; } = TypeStatus.Initialized;

        public bool? IsQueryType { get; set; }

        public bool? IsMutationType { get; set; }

        public bool? IsSubscriptionType { get; set; }

        public IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        public IsOfTypeFallback IsOfType { get; }

        public ITypeSystemObject Type => _initializationContext.Type;

        public string Scope => _initializationContext.Scope;

        public bool IsType => _initializationContext.IsType;

        public bool IsIntrospectionType => _initializationContext.IsIntrospectionType;

        public bool IsDirective => _initializationContext.IsDirective;

        public bool IsSchema => _initializationContext.IsSchema;

        public IServiceProvider Services => _initializationContext.Services;

        public IDictionary<string, object> ContextData => _initializationContext.ContextData;

        public IDescriptorContext DescriptorContext => _initializationContext.DescriptorContext;

        public ITypeInterceptor TypeInterceptor => _initializationContext.TypeInterceptor;

        public ITypeInspector TypeInspector => _initializationContext.TypeInspector;

        /// <inheritdoc />
        public bool TryGetType<T>(ITypeReference typeRef, out T type)
            where T : IType
        {
            if (_typeReferenceResolver.TryGetType(typeRef, out IType t) &&
                t is T casted)
            {
                type = casted;
                return true;
            }

            type = default;
            return false;
        }

        /// <inheritdoc />
        public T GetType<T>(ITypeReference typeRef)
            where T : IType
        {
            if (typeRef is null)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            if (!TryGetType(typeRef, out T type))
            {
                throw TypeCompletionContext_UnableToResolveType(Type, typeRef);
            }

            return type;
        }

        public ITypeReference GetNamedTypeReference(ITypeReference typeRef)
            => _typeReferenceResolver.GetNamedTypeReference(typeRef);

        public bool TryGetDirectiveType(
            IDirectiveReference directiveRef,
            [NotNullWhen(true)] out DirectiveType directiveType) =>
            _typeReferenceResolver.TryGetDirectiveType(directiveRef, out directiveType);

        public DirectiveType GetDirectiveType(IDirectiveReference directiveRef)
        {
            return _typeReferenceResolver.TryGetDirectiveType(
                directiveRef,
                out DirectiveType directiveType)
                ? directiveType
                : null;
        }

        public Func<ISchema> GetSchemaResolver()
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            return _schemaResolver;
        }

        public IEnumerable<T> GetTypes<T>()
            where T : IType
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            return _typeReferenceResolver.GetTypes<T>();
        }

        public void ReportError(ISchemaError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _initializationContext.ReportError(error);
        }

        public bool TryPredictTypeKind(ITypeReference typeRef, out TypeKind kind) =>
            _initializationContext.TryPredictTypeKind(typeRef, out kind);
    }
}
