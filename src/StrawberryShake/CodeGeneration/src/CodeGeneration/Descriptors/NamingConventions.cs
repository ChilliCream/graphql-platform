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

        public static string TypeDeserializeMethodNameFromTypeName(TypeReferenceDescriptor namedTypeReferenceDescriptor)
        {
            var ret = "Deserialize";
            ret += namedTypeReferenceDescriptor.IsNullable ? "Nullable" : "NonNullable";
            ret += namedTypeReferenceDescriptor.Type.Name.WithCapitalFirstChar();
            return ret;
        }

        public static string TypeUpdateMethodNameFromTypeName(TypeReferenceDescriptor namedTypeReferenceDescriptor)
        {
            var ret = "Update";
            ret += EntityTypeNameFromTypeName(namedTypeReferenceDescriptor.Type.Name);
            return ret;
        }
    }
}
