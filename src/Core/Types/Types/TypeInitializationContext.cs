using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types
{
    internal class TypeInitializationContext
        : ITypeInitializationContext
    {
        private readonly ISchemaContext _schemaContext;
        private readonly Action<SchemaError> _reportError;

        public TypeInitializationContext(ISchemaContext schemaContext,
            Action<SchemaError> reportError, INamedType namedType,
            bool isQueryType)
        {
            _schemaContext = schemaContext
                ?? throw new ArgumentNullException(nameof(schemaContext));
            _reportError = reportError
                ?? throw new ArgumentNullException(nameof(reportError));
            Type = namedType
                ?? throw new ArgumentNullException(nameof(namedType));
            IsQueryType = isQueryType;
        }

        public TypeInitializationContext(ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            _schemaContext = schemaContext
                ?? throw new ArgumentNullException(nameof(schemaContext));
            _reportError = reportError
                ?? throw new ArgumentNullException(nameof(reportError));
            IsDirective = true;
        }

        public INamedType Type { get; }

        public bool IsQueryType { get; }

        public bool IsDirective { get; }

        public IServiceProvider Services => _schemaContext.Services;

        public IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType)
        {
            if (abstractType == null)
            {
                throw new ArgumentNullException(nameof(abstractType));
            }

            if (abstractType is InterfaceType)
            {
                return GetPossibleInterfaceTypes(abstractType).ToList();
            }

            if (abstractType is UnionType ut)
            {
                return ut.Types.Values.ToList();
            }

            throw new NotSupportedException(
                "The specified type is not a supported abstract type.");
        }

        private IEnumerable<ObjectType> GetPossibleInterfaceTypes(
            INamedType abstractType)
        {
            foreach (ObjectType objectType in _schemaContext.Types
                .GetTypes().OfType<ObjectType>())
            {
                if (objectType.Interfaces.ContainsKey(abstractType.Name))
                {
                    yield return objectType;
                }
            }
        }

        public FieldResolverDelegate GetResolver(NameString fieldName)
        {
            fieldName.EnsureNotEmpty(nameof(fieldName));

            if (Type is ObjectType t
                && t.Fields.ContainsField(fieldName))
            {
                return _schemaContext.Resolvers.GetResolver(t.Name, fieldName);
            }
            return null;
        }

        public T GetType<T>(TypeReference typeReference) where T : IType
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            return _schemaContext.Types.GetType<T>(typeReference);
        }

        public void RegisterResolver(
            Type sourceType, Type resolverType,
            NameString fieldName, MemberInfo fieldMember)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (fieldMember == null)
            {
                throw new ArgumentNullException(nameof(fieldMember));
            }

            RegisterResolverInternal(
                sourceType,
                resolverType,
                fieldName.EnsureNotEmpty(nameof(fieldName)),
                fieldMember);
        }

        private void RegisterResolverInternal(
            Type sourceType, Type resolverType,
            NameString fieldName, MemberInfo fieldMember)
        {
            if (resolverType == null)
            {
                _schemaContext.Resolvers.RegisterResolver(
                    new FieldMember(Type.Name, fieldName, fieldMember));
            }
            else
            {
                _schemaContext.Resolvers.RegisterResolver(
                    new ResolverDescriptor(resolverType, sourceType,
                        new FieldMember(Type.Name, fieldName, fieldMember)));
            }
        }

        public void RegisterType(
            INamedType namedType, ITypeBinding typeBinding = null)
        {
            if (namedType == null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            _schemaContext.Types.RegisterType(namedType, typeBinding);
        }

        public void RegisterType(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            _schemaContext.Types.RegisterType(typeReference);
        }

        public void ReportError(SchemaError error) => _reportError(error);

        public bool TryGetNativeType(INamedType namedType, out Type nativeType)
        {
            if (_schemaContext.Types.TryGetTypeBinding(
                namedType, out ITypeBinding binding))
            {
                nativeType = binding.Type;
                return true;
            }

            nativeType = null;
            return false;
        }

        public bool TryGetProperty<T>(
            INamedType namedType, NameString fieldName, out T member)
            where T : MemberInfo
        {
            if (namedType is ObjectType
                && _schemaContext.Types.TryGetTypeBinding(namedType,
                    out ObjectTypeBinding binding)
                && binding.Fields.TryGetValue(fieldName,
                    out FieldBinding fieldBinding)
                && fieldBinding.Member is T m)
            {
                member = m;
                return true;
            }


            if (namedType is InputObjectType
                && _schemaContext.Types.TryGetTypeBinding(namedType,
                    out InputObjectTypeBinding inputBinding)
                && inputBinding.Fields.TryGetValue(fieldName,
                    out InputFieldBinding inputFieldBinding)
                && inputFieldBinding.Property is T p)
            {
                member = p;
                return true;

            }

            member = null;
            return false;
        }

        public DirectiveType GetDirectiveType(
            DirectiveReference directiveReference)
        {
            if (directiveReference == null)
            {
                throw new ArgumentNullException(nameof(directiveReference));
            }

            return _schemaContext.Directives
                .GetDirectiveType(directiveReference);
        }

        public void RegisterMiddleware(IDirectiveMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _schemaContext.Resolvers.RegisterMiddleware(middleware);
        }

        public IDirectiveMiddleware GetMiddleware(string directiveName)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            return _schemaContext.Resolvers.GetMiddleware(directiveName);
        }

        public IEnumerable<Type> GetResolverTypes(NameString typeName)
        {
            return _schemaContext.Types.GetResolverTypes(typeName);
        }

        public FieldResolverDelegate CreateFieldMiddleware(
            IEnumerable<FieldMiddleware> mappedMiddlewareComponents,
            FieldResolverDelegate fieldResolver)
        {
            return _schemaContext.Resolvers.CreateMiddleware(
                mappedMiddlewareComponents,
                fieldResolver);
        }
    }
}
