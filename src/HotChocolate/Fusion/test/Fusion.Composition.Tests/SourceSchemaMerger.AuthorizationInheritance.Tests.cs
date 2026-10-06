namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerAuthorizationInheritanceTests : SourceSchemaMergerTestBase
{
    private const string AuthorizationDirectives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Merge_Should_InheritInterfaceFieldRequirement_When_ImplementationIsUnannotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node: Node
                }

                interface Node {
                    id: ID! @requiresScopes(scopes: [["read"]])
                }

                type User implements Node {
                    id: ID!
                    name: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node: Node @fusion__field(schema: A)
            }

            type User implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__authorization(scopes: [["read"]]) @fusion__field(schema: A)
              name: String @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__authorization(scopes: [["read"]]) @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_CombineInterfaceAndImplementationRequirements_When_BothDeclareScopes()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node: Node
                }

                interface Node {
                    id: ID! @requiresScopes(scopes: [["read"], ["audit"]])
                }

                type User implements Node {
                    id: ID! @requiresScopes(scopes: [["admin"]]) @authenticated
                    name: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node: Node @fusion__field(schema: A)
            }

            type User implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID!
                @fusion__authorization(
                  authenticated: true
                  scopes: [["admin", "audit"], ["admin", "read"]]
                )
                @fusion__field(schema: A)
              name: String @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID!
                @fusion__authorization(
                  authenticated: true
                  scopes: [["admin", "audit"], ["admin", "read"]]
                )
                @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_FlattenTypeLevelRequirementOntoEveryField_When_ObjectIsAnnotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    me: Account
                }

                type Account @requiresScopes(scopes: [["account"]]) {
                    id: ID!
                    email: String @policy(policies: [["Owner"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              me: Account
                @fusion__authorization(scopes: [["account"]])
                @fusion__field(schema: A)
            }

            type Account
              @fusion__authorization(scopes: [["account"]])
              @fusion__type(schema: A) {
              email: String
                @fusion__authorization(scopes: [["account"]], policies: [["Owner"]])
                @fusion__field(schema: A)
              id: ID!
                @fusion__authorization(scopes: [["account"]])
                @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_FenceSiblingImplementation_When_OneImplementationIsProtected()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    search: [SearchResult!]!
                    video: Video
                }

                interface SearchResult {
                    title: String
                }

                type Article implements SearchResult {
                    title: String @authenticated
                    body: String
                }

                type Video implements SearchResult {
                    title: String
                    url: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              search: [SearchResult!]! @fusion__field(schema: A)
              video: Video @fusion__field(schema: A)
            }

            type Article implements SearchResult
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "SearchResult") {
              body: String @fusion__field(schema: A)
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }

            type Video implements SearchResult
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "SearchResult") {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
              url: String @fusion__field(schema: A)
            }

            interface SearchResult @fusion__type(schema: A) {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_FoldTypeRequirementThroughFieldsOnly_When_ImplementationIsAnnotatedOnTheType()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    search: [SearchResult!]!
                    video: Video
                }

                interface SearchResult {
                    title: String
                }

                type Article implements SearchResult @authenticated {
                    title: String
                }

                type Video implements SearchResult {
                    title: String
                    url: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              search: [SearchResult!]! @fusion__field(schema: A)
              video: Video @fusion__field(schema: A)
            }

            type Article implements SearchResult
              @fusion__authorization(authenticated: true)
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "SearchResult") {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }

            type Video implements SearchResult
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "SearchResult") {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
              url: String @fusion__field(schema: A)
            }

            interface SearchResult @fusion__type(schema: A) {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_InheritInterfaceTypeRequirement_When_ImplementationIsUnannotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    video: Video
                }

                interface SearchResult @authenticated {
                    title: String
                }

                type Video implements SearchResult {
                    title: String
                    url: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              video: Video
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }

            type Video implements SearchResult
              @fusion__authorization(authenticated: true)
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "SearchResult") {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
              url: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }

            interface SearchResult
              @fusion__authorization(authenticated: true)
              @fusion__type(schema: A) {
              title: String
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_ProtectFieldsReturningTheType_When_NamedTypesAreAnnotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    secret: Secret
                    level: Level
                    token: Token
                    thing: Thing
                    many: [Secret!]!
                }

                type Secret @authenticated {
                    id: ID!
                }

                enum Level @requiresScopes(scopes: [["level"]]) {
                    LOW
                }

                scalar Token @policy(policies: [["TokenPolicy"]])

                interface Thing @authenticated @requiresScopes(scopes: [["thing"]]) {
                    id: ID!
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              level: Level
                @fusion__authorization(scopes: [["level"]])
                @fusion__field(schema: A)
              many: [Secret!]!
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
              secret: Secret
                @fusion__authorization(authenticated: true)
                @fusion__field(schema: A)
              thing: Thing
                @fusion__authorization(authenticated: true, scopes: [["thing"]])
                @fusion__field(schema: A)
              token: Token
                @fusion__authorization(policies: [["TokenPolicy"]])
                @fusion__field(schema: A)
            }

            type Secret @fusion__authorization(authenticated: true) @fusion__type(schema: A) {
              id: ID! @fusion__authorization(authenticated: true) @fusion__field(schema: A)
            }

            interface Thing
              @fusion__authorization(authenticated: true, scopes: [["thing"]])
              @fusion__type(schema: A) {
              id: ID!
                @fusion__authorization(authenticated: true, scopes: [["thing"]])
                @fusion__field(schema: A)
            }

            enum Level @fusion__authorization(scopes: [["level"]]) @fusion__type(schema: A) {
              LOW @fusion__enumValue(schema: A)
            }

            scalar Token
              @fusion__authorization(policies: [["TokenPolicy"]])
              @fusion__type(schema: A)
            """);
    }

    [Fact]
    public void Merge_Should_ExpandInterfaceObjectFieldRequirement_When_StandInFieldIsAnnotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    media: Media
                }

                interface Media @key(fields: "id") {
                    id: ID!
                    title: String
                }

                type Book implements Media @key(fields: "id") {
                    id: ID!
                    title: String
                    pages: Int @requiresScopes(scopes: [["pages"]])
                }

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [String!]! @requiresScopes(scopes: [["reviews"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              media: Media @fusion__field(schema: A)
            }

            type Book implements Media
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Media") {
              id: ID! @fusion__field(schema: A)
              pages: Int
                @fusion__authorization(scopes: [["pages"]])
                @fusion__field(schema: A)
              reviews: [String!]!
                @fusion__authorization(scopes: [["reviews"]])
                @fusion__field(schema: B)
              title: String @fusion__field(schema: A)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              reviews: [String!]!
                @fusion__authorization(scopes: [["reviews"]])
                @fusion__field(schema: B)
              title: String @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_InheritThroughInterfaceChain_When_InterfaceImplementsInterface()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node: Node
                }

                interface Node {
                    id: ID! @requiresScopes(scopes: [["node"]])
                }

                interface Entity implements Node {
                    id: ID!
                    name: String @policy(policies: [["EntityPolicy"]])
                }

                type User implements Entity & Node {
                    id: ID!
                    name: String
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node: Node @fusion__field(schema: A)
            }

            type User implements Entity & Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Entity")
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__authorization(scopes: [["node"]]) @fusion__field(schema: A)
              name: String
                @fusion__authorization(policies: [["EntityPolicy"]])
                @fusion__field(schema: A)
            }

            interface Entity implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__authorization(scopes: [["node"]]) @fusion__field(schema: A)
              name: String
                @fusion__authorization(policies: [["EntityPolicy"]])
                @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__authorization(scopes: [["node"]]) @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_CombineRequirementsOfAllSourceSchemas_When_InterfaceAndImplementationAreSplit()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node: Node
                }

                interface Node {
                    id: ID! @requiresScopes(scopes: [["a"]])
                }

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    user: User
                }

                interface Node {
                    id: ID!
                }

                type User implements Node {
                    id: ID! @requiresScopes(scopes: [["b"], ["c"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              node: Node @fusion__field(schema: A)
              user: User @fusion__field(schema: B)
            }

            type User implements Node
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Node") {
              id: ID!
                @fusion__authorization(scopes: [["a", "b"], ["a", "c"]])
                @fusion__field(schema: B)
            }

            interface Node @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID!
                @fusion__authorization(scopes: [["a", "b"], ["a", "c"]])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_NotProtectNodeTypeOrSiblings_When_NodeImplementationIsAnnotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node(id: ID!): Node @lookup
                    product(id: ID!): Product @lookup
                }

                interface Node {
                    id: ID!
                }

                type User implements Node @authenticated {
                    id: ID!
                }

                type Product implements Node {
                    id: ID!
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID!): Node @fusion__gateway_field
              product(id: ID! @fusion__inputField(schema: A)): Product
                @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "product(id: ID!): Product"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__authorization(authenticated: true) @fusion__field(schema: A)
            }

            type User implements Node
              @fusion__authorization(authenticated: true)
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__authorization(authenticated: true) @fusion__field(schema: A)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__authorization(authenticated: true) @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }

    [Fact]
    public void Merge_Should_KeepNodeFieldRequirement_When_SourceNodeFieldIsAnnotated()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    node(id: ID!): Node @lookup @requiresScopes(scopes: [["node"]])
                }

                interface Node {
                    id: ID!
                }

                type User implements Node {
                    id: ID!
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID!): Node
                @fusion__authorization(scopes: [["node"]])
                @fusion__gateway_field
            }

            type User implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }
}
