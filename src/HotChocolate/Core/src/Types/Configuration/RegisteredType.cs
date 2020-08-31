using System.Linq;
using System;
using System.Collections.Generic;
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
                        "The completion context has not been initialized.");
                }

                return _completionContext;
            }
        }

        public bool IsInferred { get; }

        public bool IsExtension { get; }

        public bool IsNamedType { get; }

        public bool IsDirectiveType { get; }

        public void AddReferences(
            IReadOnlyList<ITypeReference> references)
        {
            var merged = References.ToList();
            merged.AddRange(references);
            _references = merged;
        }

        public void AddDependencies(
            IReadOnlyList<TypeDependency> dependencies)
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
                    "The completion context can only be set once.");
            }

            _completionContext = completionContext;
        }
    }
}
