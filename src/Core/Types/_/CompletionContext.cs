using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal sealed class CompletionContext
        : ICompletionContext
    {
        private readonly InitializationContext _initializationContext;
        private readonly TypeInitializer _typeInitializer;
        private readonly Dictionary<ITypeReference, TypeDependencyKind> _deps =
            new Dictionary<ITypeReference, TypeDependencyKind>();

        public CompletionContext(
            InitializationContext initializationContext,
            TypeInitializer typeInitializer)
        {
            _initializationContext = initializationContext
                ?? throw new ArgumentNullException(
                    nameof(initializationContext));
            _typeInitializer = typeInitializer
                ?? throw new ArgumentNullException(
                    nameof(typeInitializer));

            foreach (TypeDependency dependency in
                _initializationContext.TypeDependencies)
            {
                if (!_deps.ContainsKey(dependency.TypeReference))
                {
                    _deps.Add(dependency.TypeReference, dependency.Kind);
                }
            }
        }

        public TypeStatus Status { get; set; } = TypeStatus.Initialized;

        public bool? IsQueryType { get; set; }

        public IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        public ITypeSystemObject Type => _initializationContext.Type;

        public bool IsType => _initializationContext.IsType;

        public bool IsIntrospectionType =>
            _initializationContext.IsIntrospectionType;

        public bool IsDirective => _initializationContext.IsDirective;

        public IServiceProvider Services => _initializationContext.Services;

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

            if (_deps.TryGetValue(reference, out TypeDependencyKind kind))
            {
                if (Status == TypeStatus.Initialized &&
                    (kind == TypeDependencyKind.Completed
                    || kind == TypeDependencyKind.Default))
                {
                    // TODO : resources
                    throw new InvalidOperationException(
                        "This dependency is registered for the completed stage!");
                }

                if (_typeInitializer.DependencyLookup.TryGetValue(
                    reference, out ITypeReference nr)
                    && _typeInitializer.Types.TryGetValue(
                        nr, out RegisteredType rt)
                    && rt.Type is T t)
                {
                    type = t;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public DirectiveType GetDirectiveType(IDirectiveReference reference)
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            throw new NotImplementedException();
        }

        public FieldResolver GetResolver(IFieldReference reference)
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            if ((_typeInitializer.Resolvers.TryGetValue(
                new FieldReference(reference.TypeName, reference.FieldName),
                out RegisteredResolver resolver)
                || _typeInitializer.Resolvers.TryGetValue(
                new FieldReference(
                    _initializationContext.InternalName,
                    reference.FieldName),
                out resolver))
                && resolver.Field is FieldResolver res)
            {
                return res;
            }

            return null;
        }

        public Func<ISchema> GetSchemaResolver()
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            throw new NotImplementedException();
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
