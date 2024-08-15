using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @shareable repeatable on FIELD_DEFINITION | OBJECT
/// </code>
///
/// The @shareable directive indicates that given object and/or field can be resolved by multiple subgraphs.
/// If an object is marked as @shareable then all its fields are automatically shareable without the need
/// for explicitly marking them with @shareable directive. All fields referenced from @key directive are
/// automatically shareable as well.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!                           # shareable because id is a key field
///   name: String                      # non-shareable
///   description: String @shareable    # shareable
/// }
///
/// type Bar @shareable {
///   description: String               # shareable because User is marked shareable
///   name: String                      # shareable because User is marked shareable
/// }
/// </example>
/// </summary>
[Package(Federation20)]
[DirectiveType(
    ShareableDirective_Name,
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.Object,
    IsRepeatable = true)]
[GraphQLDescription(ShareableDirective_Description)]
public sealed class ShareableDirective
{
    public static ShareableDirective Default { get; } = new();
}
