using System.Reflection;
using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    internal sealed class InitializationContext
        : IInitializationContext
    {
        private readonly List<TypeDependency> _typeDependencies =
            new List<TypeDependency>();
        private readonly List<IDirectiveReference> _directiveReferences =
            new List<IDirectiveReference>();

        private readonly IDescriptorContext _descriptorContext;

        public InitializationContext(
            ITypeSystemObject type,
            IServiceProvider services,
            IDescriptorContext descriptorContext,
            IDictionary<string, object> contextData)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
            _descriptorContext = descriptorContext
                ?? throw new ArgumentNullException(nameof(descriptorContext));
            ContextData = contextData
                ?? throw new ArgumentNullException(nameof(contextData));

            IsDirective = type is DirectiveType;

            if (type is INamedType nt)
            {
                IsType = true;
                IsIntrospectionType = nt.IsIntrospectionType();
            }

            InternalName = "Type_" + Guid.NewGuid().ToString("N");
        }

        public NameString InternalName { get; }

        public ITypeSystemObject Type { get; }

        public bool IsType { get; }

        public bool IsIntrospectionType { get; }

        public bool IsDirective { get; }

        public IServiceProvider Services { get; }

        public IReadOnlyList<TypeDependency> TypeDependencies =>
            _typeDependencies;

        public ICollection<IDirectiveReference> DirectiveReferences =>
            _directiveReferences;

        public IDictionary<FieldReference, RegisteredResolver> Resolvers
        { get; } = new Dictionary<FieldReference, RegisteredResolver>();

        public ICollection<ISchemaError> Errors { get; } =
            new List<ISchemaError>();

        public IDictionary<string, object> ContextData { get; }

        public IDescriptorContext DescriptorContext =>
            _descriptorContext;

        public void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind)
        {
            if (reference == null)
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
            if (references == null)
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
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _directiveReferences.Add(reference);
        }

        public void RegisterDependencyRange(
            IEnumerable<IDirectiveReference> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            _directiveReferences.AddRange(references);
        }

        public void RegisterResolver(
            NameString fieldName,
            MemberInfo member,
            Type sourceType,
            Type resolverType)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            fieldName.EnsureNotEmpty(nameof(fieldName));

            var fieldMember = new FieldMember(InternalName, fieldName, member);

            Resolvers[fieldMember.ToFieldReference()] = resolverType == null
                ? new RegisteredResolver(sourceType, fieldMember)
                : new RegisteredResolver(resolverType, sourceType, fieldMember);
        }

        public void RegisterResolver(
            NameString fieldName,
            Expression expression,
            Type sourceType,
            Type resolverType)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            fieldName.EnsureNotEmpty(nameof(fieldName));

            var fieldMember = new FieldMember(InternalName, fieldName, expression);

            Resolvers[fieldMember.ToFieldReference()] = resolverType == null
                ? new RegisteredResolver(sourceType, fieldMember)
                : new RegisteredResolver(resolverType, sourceType, fieldMember);
        }

        public void ReportError(ISchemaError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors.Add(error);
        }

        private static FieldReference Normalize(IFieldReference reference) =>
            new FieldReference(reference.TypeName, reference.FieldName);
    }
}
