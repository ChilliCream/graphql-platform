using System;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration
{
    public static class NamingConventions
    {
        public static string ResultInfoNameFromTypeName(string typeName)
        {
            return typeName + "Info";
        }

        public static string MutationServiceNameFromTypeName(string typeName)
        {
            return typeName + "Mutation";
        }

        public static string SubscriptionServiceNameFromTypeName(string typeName)
        {
            return typeName + "Subscription";
        }

        public static string QueryServiceNameFromTypeName(string typeName)
        {
            return typeName + "Query";
        }

        public static string EntityTypeNameFromTypeName(string typeName)
        {
            return typeName + "Entity";
        }

        public static string MapperNameFromTypeName(string typeName)
        {
            return typeName + "From" + EntityTypeNameFromTypeName(typeName) + "Mapper";
        }

        public static string RequestNameFromOperationServiceName(string operationServiceName)
        {
            return operationServiceName + "Request";
        }

        public static string ResultFactoryNameFromTypeName(string typeName)
        {
            return typeName + "Factory";
        }

        public static string ResultBuilderNameFromTypeName(string typeName)
        {
            return typeName + "Builder";
        }

        public static string DeserializerMethodNameFromTypeName(TypeReferenceDescriptor typeDescriptor)
        {
            var ret = "Update";
            if (typeDescriptor.IsNullable)
            {
                ret += "Nullable";
            }
            else
            {
                ret += "NonNullable";
            }

            ret += typeDescriptor.Type.Kind switch
            {
                TypeKind.Scalar => typeDescriptor.Type.Name.WithCapitalFirstChar(),
                TypeKind.DataType => DataTypeNameFromTypeName(typeDescriptor.Type.Name),
                TypeKind.EntityType => EntityTypeNameFromTypeName(typeDescriptor.Type.Name),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!typeDescriptor.IsListType) return ret;


            ret += typeDescriptor.ListType switch
            {
                ListType.NullableList => "Nullable",
                ListType.List => "NonNullable",
                _ => throw new ArgumentOutOfRangeException()
            };

            ret += "Array";

            return ret;
        }

        public static string DataTypeNameFromTypeName(string typeName)
        {
            return typeName + "Data";
        }
    }
}
