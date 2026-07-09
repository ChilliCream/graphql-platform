using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

internal static class IntrospectionSchema
{
    private static DocumentNode? s_document;
    private static DocumentNode? s_optInDocument;

    public static ReadOnlySpan<byte> SourceText =>
        """
        type __Schema {
          description: String
          types: [__Type!]!
          queryType: __Type!
          mutationType: __Type
          subscriptionType: __Type
          directives(includeDeprecated: Boolean! = false): [__Directive!]!
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
          # must be non-null for INPUT_OBJECT, otherwise null.
          isOneOf: Boolean
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
          isDeprecated: Boolean!
          deprecationReason: String
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
          DIRECTIVE_DEFINITION
        }
        """u8;

    public static ReadOnlySpan<byte> OptInSourceText =>
        """
        type __Schema {
          description: String
          types: [__Type!]!
          queryType: __Type!
          mutationType: __Type
          subscriptionType: __Type
          directives(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__Directive!]!
          optInFeatures: [String!]
          optInFeatureStability: [__OptInFeatureStability!]!
        }

        type __Type {
          kind: __TypeKind!
          name: String
          description: String
          # may be non-null for custom SCALAR, otherwise null.
          specifiedByURL: String
          # must be non-null for OBJECT and INTERFACE, otherwise null.
          fields(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__Field!]
          # must be non-null for OBJECT and INTERFACE, otherwise null.
          interfaces: [__Type!]
          # must be non-null for INTERFACE and UNION, otherwise null.
          possibleTypes: [__Type!]
          # must be non-null for ENUM, otherwise null.
          enumValues(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__EnumValue!]
          # must be non-null for INPUT_OBJECT, otherwise null.
          inputFields(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__InputValue!]
          # must be non-null for NON_NULL and LIST, otherwise null.
          ofType: __Type
          # must be non-null for INPUT_OBJECT, otherwise null.
          isOneOf: Boolean
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
          args(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__InputValue!]!
          type: __Type!
          isDeprecated: Boolean!
          deprecationReason: String
          requiresOptIn: [String!]
        }

        type __InputValue {
          name: String!
          description: String
          type: __Type!
          defaultValue: String
          isDeprecated: Boolean!
          deprecationReason: String
          requiresOptIn: [String!]
        }

        type __EnumValue {
          name: String!
          description: String
          isDeprecated: Boolean!
          deprecationReason: String
          requiresOptIn: [String!]
        }

        type __Directive {
          name: String!
          description: String
          isRepeatable: Boolean!
          locations: [__DirectiveLocation!]!
          args(includeDeprecated: Boolean! = false, includeOptIn: [String!]): [__InputValue!]!
          requiresOptIn: [String!]
          isDeprecated: Boolean!
          deprecationReason: String
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
          DIRECTIVE_DEFINITION
        }

        type __OptInFeatureStability {
          feature: String!
          stability: String!
        }
        """u8;

    public static DocumentNode Document
    {
        get
        {
            return s_document ??= Utf8GraphQLParser.Parse(SourceText);
        }
    }

    /// <summary>
    /// The introspection schema document extended with opt-in feature fields and arguments.
    /// Used when <c>EnableOptInFeatures</c> is <c>true</c>.
    /// </summary>
    public static DocumentNode OptInDocument
    {
        get
        {
            return s_optInDocument ??= Utf8GraphQLParser.Parse(OptInSourceText);
        }
    }
}
