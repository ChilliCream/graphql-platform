using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

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

        public IReadOnlyCollection<ObjectType> GetPossibleTypes(INamedType abstractType)
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

        private IEnumerable<ObjectType> GetPossibleInterfaceTypes(INamedType abstractType)
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

        public FieldResolverDelegate GetResolver(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "The name of the field cannot be null or empty.",
                    nameof(fieldName));
            }

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

        public void RegisterResolver(string fieldName, MemberInfo fieldMember)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "The name of the field cannot be null or empty.",
                    nameof(fieldName));
            }

            if (fieldMember == null)
            {
                throw new ArgumentNullException(nameof(fieldMember));
            }

            _schemaContext.Resolvers.RegisterResolver(
                new MemberResolverBinding(Type.Name, fieldName, fieldMember));
        }

        public void RegisterType(INamedType namedType, ITypeBinding typeBinding = null)
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

        public bool TryGetProperty<T>(INamedType namedType, string fieldName, out T member)
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
    }
}
