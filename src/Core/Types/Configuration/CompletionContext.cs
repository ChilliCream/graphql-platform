using System.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration
{
    internal sealed class CompletionContext
        : ICompletionContext
    {
        private readonly InitializationContext _initializationContext;
        private readonly TypeInitializer _typeInitializer;
        private readonly Func<ISchema> _schemaResolver;
        private readonly HashSet<NameString> _alternateNames =
            new HashSet<NameString>();

        public CompletionContext(
            InitializationContext initializationContext,
            TypeInitializer typeInitializer,
            IsOfTypeFallback isOfType,
            Func<ISchema> schemaResolver)
        {
            _initializationContext = initializationContext
                ?? throw new ArgumentNullException(
                    nameof(initializationContext));
            _typeInitializer = typeInitializer
                ?? throw new ArgumentNullException(
                    nameof(typeInitializer));
            IsOfType = isOfType;
            _schemaResolver = schemaResolver
                ?? throw new ArgumentNullException(
                    nameof(schemaResolver));

            GlobalComponents = new ReadOnlyCollection<FieldMiddleware>(
                _typeInitializer.GlobalComponents);

            _alternateNames.Add(_initializationContext.InternalName);
        }

        public TypeStatus Status { get; set; } = TypeStatus.Initialized;

        public bool? IsQueryType { get; set; }

        public IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        public IsOfTypeFallback IsOfType { get; }

        public ITypeSystemObject Type => _initializationContext.Type;

        public bool IsType => _initializationContext.IsType;

        public bool IsIntrospectionType =>
            _initializationContext.IsIntrospectionType;

        public bool IsDirective => _initializationContext.IsDirective;

        public IServiceProvider Services => _initializationContext.Services;

        public IDictionary<string, object> ContextData =>
            _initializationContext.ContextData;

        public ISet<NameString> AlternateTypeNames => _alternateNames;

        public IDescriptorContext DescriptorContext =>
            _initializationContext.DescriptorContext;

        public T GetType<T>(ITypeReference reference)
            where T : IType
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (!TryGetType(reference, out T type))
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            "Unable to resolve type reference `{0}`.",
                            reference))
                        .SetTypeSystemObject(Type)
                        .SetExtension(nameof(reference), reference)
                        .Build());
            }
            return type;
        }

        public bool TryGetType<T>(ITypeReference reference, out T type)
            where T : IType
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (reference is ISchemaTypeReference schemaRef
                && TryGetType(schemaRef, out type))
            {
                return true;
            }

            if (reference is ISyntaxTypeReference syntaxRef
                && TryGetType(syntaxRef, out type))
            {
                return true;
            }

            if (reference is IClrTypeReference clrRef
                && _typeInitializer.TryNormalizeReference(
                    clrRef, out ITypeReference normalized)
                && _typeInitializer.DiscoveredTypes is { }
                && _typeInitializer.DiscoveredTypes.TryGetType(
                    normalized, out RegisteredType registered)
                && registered.Type is IType t
                && _typeInitializer.TypeInspector.TryCreate(
                    clrRef.Type, out TypeInfo typeInfo)
                && typeInfo.TypeFactory.Invoke(t) is T casted)
            {
                type = casted;
                return true;
            }

            type = default;
            return false;
        }

        private bool TryGetType<T>(
            ISchemaTypeReference reference,
            out T type)
            where T : IType
        {
            if (reference.Type is IType schemaType)
            {
                INamedType namedType = schemaType.NamedType();
                if (_typeInitializer.DiscoveredTypes is { }
                    && _typeInitializer.DiscoveredTypes.Types.Any(t => t.Type == namedType)
                    && schemaType is T casted)
                {
                    type = casted;
                    return true;
                }
            }

            type = default;
            return false;
        }

        private bool TryGetType<T>(
            ISyntaxTypeReference reference,
            out T type)
            where T : IType
        {
            NamedTypeNode namedType = reference.Type.NamedType();
            if (_typeInitializer.TryGetType(namedType.Name.Value, out IType t)
                && WrapType(t, reference.Type) is T casted)
            {
                type = casted;
                return true;
            }

            type = default;
            return false;
        }

        private static IType WrapType(
           IType namedType,
           ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nntn)
            {
                return new NonNullType(WrapType(namedType, nntn.Type));
            }
            else if (typeNode is ListTypeNode ltn)
            {
                return new ListType(WrapType(namedType, ltn.Type));
            }
            else
            {
                return namedType;
            }
        }

        public DirectiveType GetDirectiveType(IDirectiveReference reference)
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            if (reference is ClrTypeDirectiveReference cr)
            {
                var clrTypeReference = new ClrTypeReference(
                    cr.ClrType, TypeContext.None);
                if (!_typeInitializer.ClrTypes.TryGetValue(
                    clrTypeReference,
                    out ITypeReference internalReference))
                {
                    internalReference = clrTypeReference;
                }

                if (_typeInitializer.TryGetRegisteredType(
                    internalReference,
                    out RegisteredType registeredType))
                {
                    return (DirectiveType)registeredType.Type;
                }
            }

            if (reference is NameDirectiveReference nr
                && _typeInitializer.DiscoveredTypes is { })
            {
                return _typeInitializer.DiscoveredTypes.Types
                    .Select(t => t.Type)
                    .OfType<DirectiveType>()
                    .FirstOrDefault(t => t.Name.Equals(nr.Name));
            }

            return null;
        }

        public FieldResolver GetResolver(NameString fieldName)
        {
            fieldName.EnsureNotEmpty(nameof(fieldName));

            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            if (TryGetResolver(Type.Name, fieldName,
                out FieldResolver resolver))
            {
                return resolver;
            }

            foreach (NameString alternateName in _alternateNames)
            {
                if (TryGetResolver(alternateName, fieldName, out resolver))
                {
                    return resolver;
                }
            }

            return null;
        }

        private bool TryGetResolver(
            NameString typeName,
            NameString fieldName,
            out FieldResolver resolver)
        {
            if (_typeInitializer.Resolvers.TryGetValue(
                new FieldReference(typeName, fieldName),
                out RegisteredResolver rr)
                && rr.Field is FieldResolver r)
            {
                resolver = r;
                return true;
            }

            resolver = null;
            return false;
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
            if (Status == TypeStatus.Initialized
                || _typeInitializer.DiscoveredTypes is null)
            {
                throw new NotSupportedException();
            }

            return _typeInitializer.DiscoveredTypes.Types
                .Select(t => t.Type)
                .OfType<T>()
                .Distinct();
        }

        public void ReportError(ISchemaError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _initializationContext.ReportError(error);
        }
    }
}
