using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.CSharp
{
    public class TypeLookup
        : ITypeLookup
    {
        private static readonly Dictionary<Type, string> _aliases =
            new Dictionary<Type, string>
            {
                { typeof(string), "string" },
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(short), "short" },
                { typeof(int), "int" },
                { typeof(long), "long" },
                { typeof(ushort), "ushort" },
                { typeof(uint), "uint" },
                { typeof(ulong), "ulong" },
                { typeof(decimal), "decimal" },
                { typeof(float), "float" },
                { typeof(double), "double" }
            };

        private readonly IReadOnlyDictionary<string, LeafTypeInfo> _leafTypes;
        private readonly IReadOnlyDictionary<FieldNode, string> _generatedTypes;
        private readonly LanguageVersion _languageVersion;
        private readonly bool _nullableRef;

        public TypeLookup(
            LanguageVersion languageVersion,
            IReadOnlyDictionary<FieldNode, string> generatedTypes)
        {
            _languageVersion = languageVersion;

            _generatedTypes = generatedTypes
                ?? throw new ArgumentNullException(nameof(generatedTypes));

            _leafTypes = new[]
            {
                new LeafTypeInfo("String", typeof(string), typeof(string)),
                new LeafTypeInfo("Byte", typeof(byte), typeof(byte)),
                new LeafTypeInfo("Short", typeof(short), typeof(short)),
                new LeafTypeInfo("Int", typeof(int), typeof(int)),
                new LeafTypeInfo("Long", typeof(long), typeof(long)),
                new LeafTypeInfo("Float", typeof(double), typeof(double)),
                new LeafTypeInfo("Boolean", typeof(bool), typeof(bool)),
                new LeafTypeInfo("ID", typeof(string), typeof(string)),
                new LeafTypeInfo("Url", typeof(Uri), typeof(string)),
                new LeafTypeInfo("Date", typeof(DateTime), typeof(string)),
                new LeafTypeInfo("DateTime", typeof(DateTimeOffset), typeof(string)),
                new LeafTypeInfo("Guid", typeof(Guid), typeof(string)),
                new LeafTypeInfo("Uuid", typeof(Guid), typeof(string)),
            }.ToDictionary(t => t.TypeName);

            _nullableRef = _languageVersion == LanguageVersion.CSharp_8_0;
        }

        public TypeLookup(
            LanguageVersion languageVersion,
            IEnumerable<LeafTypeInfo> leafTypes,
            IReadOnlyDictionary<FieldNode, string> generatedTypes)
        {
            if (leafTypes is null)
            {
                throw new ArgumentNullException(nameof(leafTypes));
            }

            _languageVersion = languageVersion;
            _leafTypes = leafTypes.ToDictionary(t => t.TypeName);
            _generatedTypes = generatedTypes
                ?? throw new ArgumentNullException(nameof(generatedTypes));
            _nullableRef = _languageVersion == LanguageVersion.CSharp_8_0;
        }

        public string GetTypeName(IType fieldType, FieldNode? field, bool readOnly)
        {
            if (fieldType.NamedType().IsLeafType())
            {
                return GetLeafClrTypeName(fieldType);
            }

            if (field is null)
            {
                throw new InvalidOperationException(
                    "The type cannot be null if the field type " +
                    "is not a scalar type.");
            }

            if (!_generatedTypes.TryGetValue(field, out string? typeName))
            {
                throw new NotSupportedException(
                    $"Could not resolve type for field `{field.Name.Value}` " +
                    $"of type `{fieldType.Visualize()}`.");
            }

            return BuildType(typeName, fieldType, readOnly, true);
        }

        public string GetTypeName(IType fieldType, string? typeName, bool readOnly)
        {
            if (fieldType.NamedType().IsLeafType())
            {
                return GetLeafClrTypeName(fieldType);
            }

            if (typeName is null)
            {
                throw new InvalidOperationException(
                    "The type name cannot be null if the field type " +
                    "is not a scalar type.");
            }

            return BuildType(typeName, fieldType, readOnly, true);
        }

        public ITypeInfo GetTypeInfo(IType fieldType, bool readOnly)
        {
            INamedType namedType = fieldType.NamedType();

            if (namedType.IsEnumType())
            {
                var typeInfo = new TypeInfo
                (
                    BuildType(namedType.Name, fieldType, readOnly, true),
                    namedType.Name,
                    typeof(string),
                    fieldType
                );

                BuildTypeInfo(true, fieldType, true, readOnly, typeInfo);


                return typeInfo;
            }

            if (namedType.IsScalarType())
            {
                if (!_leafTypes.TryGetValue(namedType.Name, out LeafTypeInfo? type))
                {
                    throw new NotSupportedException(
                        $"Leaf type `{namedType.Name}` is not supported.");
                }

                var typeInfo = new TypeInfo
                (
                    BuildType(type.ClrType, fieldType, readOnly),
                    namedType.Name,
                    type.SerializationType,
                    fieldType
                );

                BuildTypeInfo(type.ClrType, fieldType, readOnly, typeInfo);

                return typeInfo;
            }

            throw new NotSupportedException(
                "Type infos are only supported for leaf types.");
        }

        private string BuildType(Type type, IType fieldType, bool readOnly)
        {
            return BuildType(type, fieldType, true, readOnly);
        }

        private string BuildType(Type type, IType fieldType, bool nullable, bool readOnly)
        {
            if (fieldType is NonNullType nnt)
            {
                return BuildType(type, nnt.Type, false, readOnly);
            }

            if (fieldType is ListType lt)
            {
                string elementType = BuildType(type, lt.ElementType, true, readOnly);

                return readOnly
                    ? string.Format("IReadOnlyList<{0}>", elementType)
                    : string.Format("List<{0}>", elementType);
            }

            if (_languageVersion == LanguageVersion.CSharp_7_3)
            {
                return nullable && type.IsValueType
                    ? GetTypeName(type) + "?"
                    : GetTypeName(type);
            }

            return nullable
                ? GetTypeName(type) + "?"
                : GetTypeName(type);
        }

        private void BuildTypeInfo(
            Type type,
            IType fieldType,
            bool readOnly,
            TypeInfo typeInfo) =>
            BuildTypeInfo(type.IsValueType, fieldType, true, readOnly, typeInfo);

        private static void BuildTypeInfo(
            bool isValueType,
            IType fieldType,
            bool nullable,
            bool readOnly,
            TypeInfo typeInfo)
        {
            if (fieldType is NonNullType nnt)
            {
                BuildTypeInfo(isValueType, nnt.Type, false, readOnly, typeInfo);
                return;
            }

            if (fieldType is ListType lt)
            {
                typeInfo.ListLevel++;
                BuildTypeInfo(isValueType, lt.ElementType, true, readOnly, typeInfo);
                typeInfo.IsValueType = false;
                return;
            }

            typeInfo.IsNullable = nullable;
            typeInfo.IsValueType = isValueType;
        }

        private string BuildType(
            string typeName,
            IType fieldType,
            bool readOnly,
            bool isNullable)
        {
            if (fieldType is NonNullType nnt)
            {
                return BuildType(typeName, nnt.Type, readOnly, false);
            }

            if (fieldType is ListType lt)
            {
                string elementType = BuildType(typeName, lt.ElementType, readOnly, false);
                string listType = readOnly
                    ? $"IReadOnlyList<{elementType}>"
                    : $"List<{elementType}>";
                return _languageVersion == LanguageVersion.CSharp_8_0
                    ? isNullable ? listType + "?" : listType
                    : listType;
            }

            return _languageVersion == LanguageVersion.CSharp_8_0
                ? isNullable ? typeName + "?" : typeName
                : typeName;
        }


        public string GetLeafClrTypeName(IType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.NamedType().IsLeafType())
            {
                throw new ArgumentException(
                    "Only leaf types are allowed.",
                    nameof(type));
            }

            return CreateLeafClrTypeName(type, true);
        }

        private string CreateLeafClrTypeName(IType type, bool nullable)
        {
            if (type.IsNonNullType())
            {
                return CreateLeafClrTypeName(type.InnerType(), false);
            }

            if (type.IsListType())
            {
                string elementType = CreateLeafClrTypeName(type.ElementType(), true);
                string listType = $"IReadOnlyList<{elementType}>";

                return _nullableRef && nullable
                    ? listType + "?"
                    : listType;
            }

            string typeName = type.NamedType().Name;

            if (type.IsEnumType())
            {
                return nullable ? typeName + "?" : typeName;
            }

            if (!_leafTypes.TryGetValue(typeName, out LeafTypeInfo? typeInfo))
            {
                throw new NotSupportedException(
                    $"Leaf type `{typeName}` is not supported.");
            }

            string clrTypeName = GetTypeName(typeInfo.ClrType);
            return (_nullableRef || typeInfo.ClrType.IsValueType) && nullable
                ? clrTypeName + "?"
                : clrTypeName;
        }

        public Type GetSerializationType(IType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            INamedType namedType = type.NamedType();

            if (!namedType.IsLeafType())
            {
                throw new ArgumentException(
                    "Only leaf types are allowed.",
                    nameof(type));
            }

            if (namedType.IsEnumType())
            {
                return typeof(string);
            }

            if (!_leafTypes.TryGetValue(namedType.Name, out LeafTypeInfo? typeInfo))
            {
                throw new NotSupportedException(
                    $"Leaf type `{namedType.Name}` is not supported.");
            }

            return typeInfo.SerializationType;
        }

        private static string GetTypeName(Type type)
        {
            if (_aliases.TryGetValue(type, out string? alias))
            {
                return alias;
            }

            return type.IsGenericType
                ? CreateGenericTypeName(type)
                : CreateTypeName(type, type.Name);
        }

        private static string CreateGenericTypeName(Type type)
        {
            string name = type.Name.Substring(0, type.Name.Length - 2);
            IEnumerable<string> arguments = type.GetGenericArguments()
                .Select(GetTypeName);
            return CreateTypeName(type,
                $"{name}<{string.Join(", ", arguments)}>");
        }

        private static string CreateTypeName(Type type, string typeName)
        {
            string? ns = GetNamespace(type);
            if (ns == null)
            {
                return typeName;
            }
            return $"{ns}.{typeName}";
        }

        private static string? GetNamespace(Type type)
        {
            if (type.IsNested)
            {
                Type declaringType = type.DeclaringType!;
                return $"{GetNamespace(declaringType!)}.{declaringType.Name}";
            }

            return type.Namespace;
        }

        private class TypeInfo
            : ITypeInfo
        {
            public TypeInfo(
                string clrTypeName,
                string schemaTypeName,
                Type serializationType,
                IType type)
            {
                ClrTypeName = clrTypeName;
                SchemaTypeName = schemaTypeName;
                SerializationType = serializationType;
                Type = type;
            }

            public string ClrTypeName { get; }

            public string SchemaTypeName { get; }

            public Type SerializationType { get; }

            public IType Type { get; }

            public int ListLevel { get; set; }

            public bool IsNullable { get; set; }

            public bool IsValueType { get; set; }
        }
    }
}
