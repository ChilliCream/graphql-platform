using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal sealed class CompletionContext
        : ICompletionContext
    {
        private readonly InitializationContext _initializationContext;
        private readonly Dictionary<ITypeReference, TypeDependencyKind> _deps =
            new Dictionary<ITypeReference, TypeDependencyKind>();
        private readonly IDictionary<ITypeReference, ITypeReference> _depsLup;
        private readonly IDictionary<ITypeReference, RegisteredType> _types;

        public CompletionContext(
            InitializationContext initializationContext,
            IReadOnlyList<FieldMiddleware> globalComponents,
            IDictionary<ITypeReference, ITypeReference> dependencyLookup,
            IDictionary<ITypeReference, RegisteredType> types)
        {
            _initializationContext = initializationContext
                ?? throw new ArgumentNullException(nameof(initializationContext));
            GlobalComponents = globalComponents
                ?? throw new ArgumentNullException(nameof(globalComponents));
            _depsLup = dependencyLookup
                ?? throw new ArgumentNullException(nameof(dependencyLookup));
            _types = types
                ?? throw new ArgumentNullException(nameof(types));

            foreach (TypeDependency dependency in
                _initializationContext.TypeDependencies)
            {
                if (!_deps.ContainsKey(dependency.TypeReference))
                {
                    _deps.Add(dependency.TypeReference, dependency.Kind);
                }
            }
        }

        public TypeStatus Status { get; } = TypeStatus.Initialized;

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

                if (_depsLup.TryGetValue(reference, out ITypeReference nr)
                    && _types.TryGetValue(nr, out RegisteredType rt)
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

        public IReadOnlyCollection<ObjectType> GetPossibleTypes()
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

            throw new NotImplementedException();
        }

        public Func<ISchema> GetSchemaResolver()
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            throw new NotImplementedException();
        }

        public IEnumerable<IType> GetTypes()
        {
            if (Status == TypeStatus.Initialized)
            {
                throw new NotSupportedException();
            }

            throw new NotImplementedException();
        }

        public void ReportError(ISchemaError error)
        {
            _initializationContext.ReportError(error);
        }
    }
}
