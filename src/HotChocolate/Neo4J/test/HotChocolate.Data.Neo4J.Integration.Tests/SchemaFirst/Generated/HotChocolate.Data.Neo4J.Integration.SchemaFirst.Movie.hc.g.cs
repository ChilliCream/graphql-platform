namespace HotChocolate.Data.Neo4J.Integration.SchemaFirst
{
    [global::System.CodeDom.Compiler.GeneratedCode("HotChocolate", "1.0.0.0")]
    public partial class Movie
    {
        public global::System.String Title { get; set; }

        [global::HotChocolate.Data.Neo4J.Neo4JRelationshipAttribute(name: "ACTED_IN", direction: global::HotChocolate.Data.Neo4J.RelationshipDirection.Incoming)]
        public global::System.Collections.Generic.List<global::HotChocolate.Data.Neo4J.Integration.SchemaFirst.Actor> Generes { get; set; }
    }
}