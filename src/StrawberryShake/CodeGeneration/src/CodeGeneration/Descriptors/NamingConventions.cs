using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public static class NamingConventions
    {
        public static string CreateResultInfoName(string typeName) =>
            typeName + "Info";

        public static string CreateMutationServiceName(string typeName) =>
            typeName + "Mutation";

        public static string CreateSubscriptionServiceName(string typeName) =>
            typeName + "Subscription";

        public static string CreateQueryServiceName(string typeName) =>
            typeName + "Query";

        public static string CreateEntityTypeName(string typeName) =>
            typeName + "Entity";

        public static string CreateDocumentTypeName(string operationTypeName) =>
            operationTypeName + "Document";

        public static NameString CreateDataMapperName(
            string typeName,
            string graphqlTypename) =>
            typeName + "From" + CreateDataTypeName(graphqlTypename) + "Mapper";

        public static NameString CreateEntityMapperName(
            string typeName,
            string graphqlTypename) =>
            typeName + "From" + CreateEntityTypeName(graphqlTypename) + "Mapper";

        public static string CreateResultFactoryName(string typeName) =>
            typeName + "Factory";

        public static string CreateResultRootTypeName(string typeName) =>
            typeName + "Result";

        public static string CreateResultBuilderName(string typeName) =>
            typeName + "Builder";

        public static string CreateDataTypeName(string typeName) =>
            typeName + "Data";

        public static string CreateEnumParserName(string enumTypeName) =>
            enumTypeName + "Serializer";

        public static string CreateServiceCollectionExtensions(string clientName) =>
            clientName + "ServiceCollectionExtensions";

        public static string CreateInputValueFormatter(InputObjectTypeDescriptor type) =>
            type.Name + "InputValueFormatter";

        public static string CreateInputValueFormatter(ScalarTypeDescriptor type) =>
            type.Name + "Serializer";
    }
}
