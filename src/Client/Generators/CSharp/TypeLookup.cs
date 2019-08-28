using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Linq;

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
                { typeof(double), "double" },
                { typeof(bool?), "bool?" },
                { typeof(byte?), "byte?" },
                { typeof(short?), "short?" },
                { typeof(int?), "int?" },
                { typeof(long?), "long?" },
                { typeof(ushort?), "ushort?" },
                { typeof(uint?), "uint?" },
                { typeof(ulong?), "ulong?" },
                { typeof(decimal?), "decimal?" },
                { typeof(float?), "float?" },
                { typeof(double?), "double?" }

            };

        private readonly IReadOnlyDictionary<string, Type> _scalarTypes;
        private readonly IReadOnlyDictionary<FieldNode, string> _generatedTypes;

        public TypeLookup(IReadOnlyDictionary<FieldNode, string> generatedTypes)
        {
            _generatedTypes = generatedTypes
                ?? throw new ArgumentNullException(nameof(generatedTypes));

            _scalarTypes = new Dictionary<string, Type>
            {
                { "String", typeof(string) },
                { "Int", typeof(int) },
                { "Float", typeof(double) },
                { "Boolean", typeof(bool) },
                { "ID", typeof(string) }
            };
        }

        public TypeLookup(
            IReadOnlyDictionary<string, Type> scalarTypes,
            IReadOnlyDictionary<FieldNode, string> generatedTypes)
        {
            _scalarTypes = scalarTypes
                ?? throw new ArgumentNullException(nameof(scalarTypes));
            _generatedTypes = generatedTypes
                ?? throw new ArgumentNullException(nameof(generatedTypes));
        }

        public string GetTypeName(FieldNode field, IType fieldType, bool readOnly)
        {
            if (fieldType.NamedType() is ScalarType scalarType)
            {
                if (!_scalarTypes.TryGetValue(scalarType.Name, out Type type))
                {
                    throw new NotSupportedException(
                        $"Scalar type `{scalarType.Name}` is not supported.");
                }
                return BuildType(type, fieldType, readOnly);
            }

            if (!_generatedTypes.TryGetValue(field, out string typeName))
            {
                throw new NotSupportedException(
                    $"Could not resolve type for field `{field.Name.Value}` " +
                    $"of type `{fieldType.Visualize()}`.");
            }

            return BuildType(typeName, fieldType, readOnly);
        }

        private static string BuildType(Type type, IType fieldType, bool readOnly)
        {
            return GetTypeName(BuildType(type, fieldType, true, readOnly));
        }

        private static Type BuildType(Type type, IType fieldType, bool nullable, bool readOnly)
        {
            if (fieldType is NonNullType nnt)
            {
                return BuildType(type, nnt.Type, false, readOnly);
            }

            if (fieldType is ListType lt)
            {
                Type elementType = BuildType(type, lt.ElementType, true, readOnly);

                return readOnly
                    ? typeof(IReadOnlyList<>).MakeGenericType(elementType)
                    : typeof(List<>).MakeGenericType(elementType);
            }

            return nullable && type.IsValueType
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;
        }

        private static string BuildType(string typeName, IType fieldType, bool readOnly)
        {
            if (fieldType is NonNullType nnt)
            {
                return BuildType(typeName, nnt.Type, readOnly);
            }

            if (fieldType is ListType lt)
            {
                string elementType = BuildType(typeName, lt.ElementType, readOnly);

                return readOnly
                    ? $"System.Collections.Generic.IReadOnlyList<{elementType}>"
                    : $"System.Collections.Generic.List<{elementType}>";
            }

            return typeName;
        }

        private static string GetTypeName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_aliases.TryGetValue(type, out string alias))
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
            string ns = GetNamespace(type);
            if (ns == null)
            {
                return typeName;
            }
            return $"{ns}.{typeName}";
        }

        private static string GetNamespace(Type type)
        {
            if (type.IsNested)
            {
                return $"{GetNamespace(type.DeclaringType)}.{type.DeclaringType.Name}";
            }
            return type.Namespace;
        }

        public string GetTypeName(IType fieldType, string typeName, bool readOnly)
        {
            throw new NotImplementedException();
        }
    }
}
