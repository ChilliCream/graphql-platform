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

        public static string EntityTypeNameFromGraphQlTypeName(string typeName)
        {
            return typeName + "Entity";
        }

        public static string DocumentTypeNameFromOperationName(string typeName)
        {
            return typeName + "Document";
        }

        public static string DataMapperNameFromGraphQlTypeName(string typeName, string graphqlTypename)
        {
            return typeName + "From" + DataTypeNameFromTypeName(graphqlTypename) + "Mapper";
        }

        public static string EntityMapperNameFromGraphQlTypeName(string typeName, string graphqlTypename)
        {
            return typeName + "From" + EntityTypeNameFromGraphQlTypeName(graphqlTypename) + "Mapper";
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

        public static string DataTypeNameFromTypeName(string typeName)
        {
            return typeName + "Data";
        }
    }
}
