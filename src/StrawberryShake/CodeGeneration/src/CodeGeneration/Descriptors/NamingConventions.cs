using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public static class NamingConventions
    {
        public static string ResultInfoNameFromTypeName(string typeName) =>
            typeName + "Info";

        public static string MutationServiceNameFromTypeName(string typeName) =>
            typeName + "Mutation";

        public static string SubscriptionServiceNameFromTypeName(string typeName) =>
            typeName + "Subscription";

        public static string QueryServiceNameFromTypeName(string typeName) =>
            typeName + "Query";

        public static string EntityTypeNameFromGraphQLTypeName(string typeName) =>
            typeName + "Entity";

        public static string DocumentTypeNameFromOperationName(string typeName) =>
            typeName + "Document";

        public static NameString DataMapperNameFromGraphQLTypeName(
            string typeName,
            string graphqlTypename) =>
            typeName + "From" + DataTypeNameFromTypeName(graphqlTypename) + "Mapper";

        public static NameString EntityMapperNameFromGraphQLTypeName(
            string typeName,
            string graphqlTypename) =>
            typeName + "From" + EntityTypeNameFromGraphQLTypeName(graphqlTypename) + "Mapper";

        public static string ResultFactoryNameFromTypeName(string typeName) =>
            typeName + "Factory";

        public static string ResultRootTypeNameFromTypeName(string typeName) =>
            typeName + "Result";

        public static string ResultBuilderNameFromTypeName(string typeName) =>
            typeName + "Builder";

        public static string DataTypeNameFromTypeName(string typeName) =>
            typeName + "Data";

        public static string EnumParserNameFromEnumName(in NameString descriptorName) =>
            descriptorName.Value + "Serializer";

        public static string ServiceCollectionExtensionsFromClientName(string clientName) =>
            clientName + "ServiceCollectionExtensions";

        public static string InputValueFormatterFromType(NamedTypeDescriptor type) =>
            type.Kind == TypeKind.InputType
                ? (type.GraphQLTypeName?.Value ?? type.Name) + "InputValueFormatter"
                : (type.GraphQLTypeName?.Value ?? type.Name) + "Serializer";
    }
}
