using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class RegisteredType
        : IHasRuntimeType
    {
        private TypeCompletionContext? _completionContext;
        private IReadOnlyList<ITypeReference> _references;
        private IReadOnlyList<TypeDependency> _dependencies;

        public RegisteredType(
            TypeSystemObjectBase type,
            IReadOnlyList<ITypeReference> references,
            IReadOnlyList<TypeDependency> dependencies,
            TypeDiscoveryContext discoveryContext,
            bool isInferred)
        {
            Type = type;
            _references = references;
            _dependencies = dependencies;
            DiscoveryContext = discoveryContext;
            IsInferred = isInferred;
            IsExtension = Type is INamedTypeExtensionMerger;
            IsNamedType = Type is INamedType;
            IsDirectiveType = Type is DirectiveType;
        }

        public TypeSystemObjectBase Type { get; }

        public Type RuntimeType
        {
            get
            {
                return Type is IHasRuntimeType hasClrType
                    ? hasClrType.RuntimeType
                    : typeof(object);
            }
        }

        public IReadOnlyList<ITypeReference> References => _references;

        public IReadOnlyList<TypeDependency> Dependencies => _dependencies;

        public TypeDiscoveryContext DiscoveryContext { get; }

        public TypeCompletionContext CompletionContext
        {
            get
            {
                if (_completionContext is null)
                {
                    throw new InvalidOperationException(
                        TypeResources.RegisteredType_CompletionContext_Not_Initialized);
                }

                return _completionContext;
            }
        }

        public bool IsInferred { get; }

        public bool IsExtension { get; }

        public bool IsNamedType { get; }

        public bool IsDirectiveType { get; }

        public void AddReferences(IEnumerable<ITypeReference> references)
        {
            var merged = _references.ToList();

            foreach (var reference in references)
            {
                if (!merged.Contains(reference))
                {
                    merged.Add(reference);
                }
            }

            _references = merged;
        }

        public void AddDependencies(IEnumerable<TypeDependency> dependencies)
        {
            var merged = Dependencies.ToList();
            merged.AddRange(dependencies);
            _dependencies = merged;
        }

        public void SetCompletionContext(TypeCompletionContext completionContext)
        {
            if (_completionContext is not null)
            {
                throw new InvalidOperationException(
                    TypeResources.RegisteredType_CompletionContext_Already_Set);
            }

            _completionContext = completionContext;
        }

        public override string? ToString()
        {
            return Type.ToString();
        }
    }
}
