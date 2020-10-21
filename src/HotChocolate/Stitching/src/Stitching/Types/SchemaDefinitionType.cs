using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Types
{
    public class SchemaDefinitionType : ObjectType<RemoteSchemaDefinition>
    {
        protected override void Configure(IObjectTypeDescriptor<RemoteSchemaDefinition> descriptor)
        {
            descriptor
                .Name(Names.SchemaDefinition)
                .BindFieldsExplicitly();

            descriptor
                .Field(t => t.Name)
                .Name(Names.Name)
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Document)
                .Name(Names.Document)
                .ResolveWith<Resolvers>(t => t.GetDocument(default!))
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(t => t.ExtensionDocuments)
                .Name(Names.ExtensionDocuments)
                .ResolveWith<Resolvers>(t => t.GetExtensionDocuments(default!))
                .Type<NonNullType<ListType<NonNullType<StringType>>>>();
        }

        private class Resolvers
        {
            public string GetDocument(
                RemoteSchemaDefinition schemaDefinition) =>
                schemaDefinition.Document.ToString(false);

            public IEnumerable<string> GetExtensionDocuments(
                RemoteSchemaDefinition schemaDefinition) =>
                schemaDefinition.ExtensionDocuments.Select(t => t.ToString(false));
        }

        public static class Names
        {
            public static NameString SchemaDefinition { get; } = "_SchemaDefinition";

            public static NameString Name { get; } = "name";

            public static NameString Document { get; } = "document";

            public static NameString ExtensionDocuments { get; } = "extensionDocuments";
        }
    }
}
