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

        public T GetType<T>(ITypeReference reference)
            where T : IType
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            TryGetType(reference, out T type);
            return type;
        }

        public bool TryGetType<T>(ITypeReference reference, out T type)
            where T : IType
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (_typeInitializer.TryNormalizeReference(
                reference, out ITypeReference nr)
                && _typeInitializer.Types.TryGetValue(
                    nr, out RegisteredType rt)
                    && rt.Type is IType t)
            {
                IType resolved;
                if (reference is IClrTypeReference cr
                    && _typeInitializer.TypeInspector.TryCreate(
                        cr.Type, out TypeInfo typeInfo))
                {
                    resolved = typeInfo.TypeFactory.Invoke(t);
                }
                else if (reference is ISyntaxTypeReference sr)
                {
                    resolved = WrapType(t, sr.Type);
                }
                else
                {
                    resolved = t;
                }

                if (resolved is T casted)
                {
                    type = casted;
                    return true;
                }
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

            if (reference is ClrTypeDirectiveReference cr
                && _typeInitializer.TryGetRegisteredType(
                    new ClrTypeReference(cr.ClrType, TypeContext.None),
                    out RegisteredType registeredType))
            {
                return (DirectiveType)registeredType.Type;
            }

            if (reference is NameDirectiveReference nr)
            {
                return _typeInitializer.Types.Values
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
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            return _typeInitializer.Types.Values
                .Select(t => t.Type).OfType<T>();
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
