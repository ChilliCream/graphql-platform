using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

internal static class IntrospectionSchema
{
    private static DocumentNode? s_document;

    public static ReadOnlySpan<byte> SourceText =>
        """
        type __Schema {
          description: String
          types: [__Type!]!
          queryType: __Type!
          mutationType: __Type
          subscriptionType: __Type
          directives: [__Directive!]!
        }

        type __Type {
          kind: __TypeKind!
          name: String
          description: String
          # may be non-null for custom SCALAR, otherwise null.
          specifiedByURL: String
          # must be non-null for OBJECT and INTERFACE, otherwise null.
          fields(includeDeprecated: Boolean! = false): [__Field!]
          # must be non-null for OBJECT and INTERFACE, otherwise null.
          interfaces: [__Type!]
          # must be non-null for INTERFACE and UNION, otherwise null.
          possibleTypes: [__Type!]
          # must be non-null for ENUM, otherwise null.
          enumValues(includeDeprecated: Boolean! = false): [__EnumValue!]
          # must be non-null for INPUT_OBJECT, otherwise null.
          inputFields(includeDeprecated: Boolean! = false): [__InputValue!]
          # must be non-null for NON_NULL and LIST, otherwise null.
          ofType: __Type
        }

        enum __TypeKind {
          SCALAR
          OBJECT
          INTERFACE
          UNION
          ENUM
          INPUT_OBJECT
          LIST
          NON_NULL
        }

        type __Field {
          name: String!
          description: String
          args(includeDeprecated: Boolean! = false): [__InputValue!]!
          type: __Type!
          isDeprecated: Boolean!
          deprecationReason: String
        }

        type __InputValue {
          name: String!
          description: String
          type: __Type!
          defaultValue: String
          isDeprecated: Boolean!
          deprecationReason: String
        }

        type __EnumValue {
          name: String!
          description: String
          isDeprecated: Boolean!
          deprecationReason: String
        }

        type __Directive {
          name: String!
          description: String
          isRepeatable: Boolean!
          locations: [__DirectiveLocation!]!
          args(includeDeprecated: Boolean! = false): [__InputValue!]!
        }

        enum __DirectiveLocation {
          QUERY
          MUTATION
          SUBSCRIPTION
          FIELD
          FRAGMENT_DEFINITION
          FRAGMENT_SPREAD
          INLINE_FRAGMENT
          VARIABLE_DEFINITION
          SCHEMA
          SCALAR
          OBJECT
          FIELD_DEFINITION
          ARGUMENT_DEFINITION
          INTERFACE
          UNION
          ENUM
          ENUM_VALUE
          INPUT_OBJECT
          INPUT_FIELD_DEFINITION
        }
        """u8;

    public static DocumentNode Document
    {
        get
        {
            return s_document ??= Utf8GraphQLParser.Parse(SourceText);
        }
    }
}
