using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal sealed class TypeDiscoveryContext : ITypeDiscoveryContext
    {
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeLookup _typeLookup;
        private readonly List<TypeDependency> _typeDependencies = new();
        private readonly List<IDirectiveReference> _directiveReferences = new();

        public TypeDiscoveryContext(
            ITypeSystemObject type,
            TypeRegistry typeRegistry,
            TypeLookup typeLookup,
            IDescriptorContext descriptorContext,
            ITypeInterceptor typeInterceptor,
            string scope)
        {
            Type = type ??
                throw new ArgumentNullException(nameof(type));
            _typeRegistry = typeRegistry ??
                throw new ArgumentNullException(nameof(typeRegistry));
            _typeLookup = typeLookup ??
                throw new ArgumentNullException(nameof(typeLookup));
            DescriptorContext = descriptorContext ??
                throw new ArgumentNullException(nameof(descriptorContext));
            TypeInterceptor = typeInterceptor ??
                throw new ArgumentNullException(nameof(typeInterceptor));
            Scope = scope;

            IsDirective = type is DirectiveType;
            IsSchema = type is Schema;

            if (type is INamedType nt)
            {
                IsType = true;
                IsIntrospectionType = nt.IsIntrospectionType();
            }

            InternalName = "Type_" + Guid.NewGuid().ToString("N");
        }

        public NameString InternalName { get; }

        public ITypeSystemObject Type { get; }

        public string Scope { get; }

        public bool IsType { get; }

        public bool IsIntrospectionType { get; }

        public bool IsDirective { get; }

        public bool IsSchema { get; }

        public IServiceProvider Services => DescriptorContext.Services;

        public IReadOnlyList<TypeDependency> TypeDependencies => _typeDependencies;

        public ICollection<IDirectiveReference> DirectiveReferences => _directiveReferences;

        public ICollection<ISchemaError> Errors { get; } =
            new List<ISchemaError>();

        public IDictionary<string, object> ContextData => DescriptorContext.ContextData;

        public IDescriptorContext DescriptorContext { get; }

        public ITypeInterceptor TypeInterceptor { get; }

        public ITypeInspector TypeInspector => DescriptorContext.TypeInspector;

        public void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind)
        {
            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _typeDependencies.Add(new TypeDependency(reference, kind));
        }

        public void RegisterDependency(TypeDependency dependency)
        {
            if (dependency is null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            _typeDependencies.Add(dependency);
        }

        public void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            foreach (ITypeReference reference in references)
            {
                _typeDependencies.Add(new TypeDependency(reference, kind));
            }
        }

        public void RegisterDependencyRange(
            IEnumerable<TypeDependency> dependencies)
        {
            _typeDependencies.AddRange(dependencies);
        }

        public void RegisterDependency(IDirectiveReference reference)
        {
            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _directiveReferences.Add(reference);
        }

        public void RegisterDependencyRange(
            IEnumerable<IDirectiveReference> references)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            _directiveReferences.AddRange(references);
        }

        public void ReportError(ISchemaError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors.Add(error);
        }

        public bool TryPredictTypeKind(ITypeReference typeRef, out TypeKind kind)
        {
            if (_typeLookup.TryNormalizeReference(typeRef, out ITypeReference namedTypeRef) &&
                _typeRegistry.TryGetType(namedTypeRef, out RegisteredType registeredType))
            {
                switch (registeredType.Type)
                {
                    case INamedType namedType:
                        kind = namedType.Kind;
                        return true;

                    case DirectiveType:
                        kind = TypeKind.Directive;
                        return true;

                    default:
                        kind = default;
                        return false;
                }
            }

            namedTypeRef ??= typeRef;

            switch (namedTypeRef)
            {
                case ExtendedTypeReference r:
                    if (Scalars.TryGetScalar(r.Type.Type, out _))
                    {
                        kind = TypeKind.Scalar;
                        return true;
                    }

                    if (r.Type.IsSchemaType)
                    {
                        kind = GetTypeKindFromSchemaType(r.Type);
                        return true;
                    }

                    return SchemaTypeResolver.TryInferSchemaTypeKind(r, out kind);

                case SchemaTypeReference r:
                    kind = GetTypeKindFromSchemaType(TypeInspector.GetType(r.Type.GetType()));
                    return true;

                default:
                    kind = default;
                    return false;
            }
        }

        private static TypeKind GetTypeKindFromSchemaType(IExtendedType type)
        {
            if (typeof(ScalarType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Scalar;
            }

            if (typeof(InputObjectType).IsAssignableFrom(type.Type))
            {
                return TypeKind.InputObject;
            }

            if (typeof(EnumType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Enum;
            }

            if (typeof(ObjectType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Object;
            }

            if (typeof(InterfaceType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Interface;
            }

            if (typeof(UnionType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Union;
            }

            if (typeof(DirectiveType).IsAssignableFrom(type.Type))
            {
                return TypeKind.Directive;
            }

            throw new NotSupportedException();
        }
    }
}
