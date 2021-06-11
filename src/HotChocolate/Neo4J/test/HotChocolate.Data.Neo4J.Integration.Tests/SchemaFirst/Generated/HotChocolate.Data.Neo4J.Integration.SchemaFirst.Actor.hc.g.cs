namespace HotChocolate.Data.Neo4J.Integration.SchemaFirst
{
    [global::System.CodeDom.Compiler.GeneratedCode("HotChocolate", "1.0.0.0")]
    public partial class Actor
    {
        public global::System.String Name { get; set; }

        [global::HotChocolate.Data.Neo4J.Neo4JRelationshipAttribute(name: "ACTED_IN", direction: global::HotChocolate.Data.Neo4J.RelationshipDirection.Outgoing)]
        public global::System.Collections.Generic.List<global::HotChocolate.Data.Neo4J.Integration.SchemaFirst.Movie> ActedIn { get; set; }
    }
}