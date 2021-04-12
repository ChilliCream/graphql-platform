using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Descriptors
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

        public static RuntimeTypeInfo CreateEntityType(
            string graphqlTypeName,
            string @namespace) =>
            new(CreateEntityTypeName(graphqlTypeName), CreateStateNamespace(@namespace));

        private static string CreateEntityTypeName(string graphqlTypeName) =>
            graphqlTypeName + "Entity";

        public static string CreateStateNamespace(string @namespace) =>
            @namespace + ".State";

        public static string CreateDocumentTypeName(string operationTypeName) =>
            operationTypeName + "Document";

        public static NameString CreateDataMapperName(
            string typeName,
            string graphqlTypename) =>
            typeName + "From" + CreateDataTypeName(graphqlTypename) + "Mapper";

        public static NameString CreateEntityMapperName(
            string typeName,
            string graphqlTypeName) =>
            typeName + "From" + CreateEntityTypeName(graphqlTypeName) + "Mapper";

        public static string CreateResultFactoryName(string typeName) =>
            typeName + "Factory";

        public static string CreateResultRootTypeName(string typeName, INamedType? type = null) =>
            type is null ? typeName + "Result" : typeName + type.Name + "Result";

        public static string CreateResultBuilderName(string typeName) =>
            typeName + "Builder";

        public static string CreateDataTypeName(string typeName) =>
            typeName + "Data";

        public static string CreateEnumParserName(string enumTypeName) =>
            enumTypeName + "Serializer";

        public static string CreateServiceCollectionExtensions(string clientName) =>
            clientName + "ServiceCollectionExtensions";

        public static string CreateStoreAccessor(string clientName) =>
            clientName + "StoreAccessor";

        public static string CreateClientProfileKind(string clientName) =>
            clientName + "ProfileKind";

        public static string CreateInputValueFormatter(InputObjectTypeDescriptor type) =>
            type.Name + "InputValueFormatter";

        public static string CreateInputValueInfo(string name) =>
            $"{NameUtils.GetInterfaceName(name)}Info";

        public static string CreateIsSetProperty(string name) =>
            $"Is{NameUtils.GetPropertyName(name)}Set";

        public static string CreateIsSetField(string name) =>
            "_set_" + NameUtils.GetParameterName(name);

        public static string CreateInputValueField(string name) =>
            "_value_" + NameUtils.GetParameterName(name);
    }
}
