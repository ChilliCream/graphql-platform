using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.Language;
using System.Collections.Generic;
using System.Linq;
using ContactDirective = HotChocolate.ApolloFederation.Contact;
using LinkDirective = HotChocolate.ApolloFederation.Link;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies @contact directive which can be used to prpvode team contact information to your subgraph schema.
    /// This information is automatically parsed and displayed by Apollo Studio. See
    /// <see href="https://www.apollographql.com/docs/graphos/graphs/federated-graphs/#contact-info-for-subgraphs">Subgraph Contact Information</see>
    /// for additional details.
    ///
    /// <code>
    /// schema @contact(description : "send urgent issues to [#oncall](https://yourteam.slack.com/archives/oncall).", name : "My Team Name", url : "https://myteam.slack.com/archives/teams-chat-room-url"){
    ///   query: Query
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// Contact title of the subgraph owner
    /// </param>
    /// <param name="url">
    /// URL where the subgraph's owner can be reached
    /// </param>
    /// <param name="description">
    /// Other relevant contact notes; supports markdown links
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor Contact(this ISchemaTypeDescriptor descriptor, string name, string? url = null, string? description = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return descriptor.Directive(new ContactDirective(name, url, description));
    }

    /// <summary>
    /// Applies @composeDirective which is used to specify custom directives that should be exposed in the
    /// Supergraph schema. If not specified, by default, Supergraph schema excludes all custom directives.
    /// <example>
    /// extend schema @composeDirective(name: "@custom")
    ///   @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
    ///   @link(url: "https://myspecs.dev/custom/v1.0", import: ["@custom"])
    ///
    /// directive @custom on FIELD_DEFINITION
    ///
    /// type Query {
    ///   helloWorld: String! @custom
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// Name of the directive that should be preserved in the supergraph composition.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor ComposeDirective(this ISchemaTypeDescriptor descriptor, string name)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return descriptor.Directive(
            WellKnownTypeNames.ComposeDirective,
            new ArgumentNode(
                WellKnownArgumentNames.Name,
                new StringValueNode(name)
            )
        );
    }

    /// <summary>
    /// Mark the type as an extension of a type that is defined by another service when
    /// using Apollo Federation.
    /// </summary>
    [Obsolete("Use ExtendsType type instead")]
    public static IObjectTypeDescriptor ExtendServiceType(
        this IObjectTypeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCreate(d => d.ContextData[ExtendMarker] = true);

        return descriptor;
    }

    /// <summary>
    /// Applies @extends directive which is used to represent type extensions in the schema. Federated extended types should have
    /// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
    ///
    /// NOTE: Federation v2 no longer requires `@extends` directive due to the smart entity type merging. All usage of @extends
    /// directive should be removed from your Federation v2 schemas.
    /// <example>
    /// # extended from the Users service
    /// type Foo @extends @key(fields: "id") {
    ///   id: ID
    ///   description: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor ExtendsType(
        this IObjectTypeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.Extends);
    }

    /// <summary>
    /// Applies the @external directive which is used to mark a field as owned by another service.
    /// This allows service A to use fields from service B while also knowing at runtime
    /// the types of that field. All the external fields should either be referenced from the @key,
    /// @requires or @provides directives field sets.
    ///
    /// Due to the smart merging of entity types, Federation v2 no longer requires @external directive
    /// on @key fields and can be safely omitted from the schema. @external directive is only required
    /// on fields referenced by the @requires and @provides directive.
    ///
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   remoteField: String @external
    ///   localField: String @requires(fields: "remoteField")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor External(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.External);
    }

    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor InterfaceObject(this IObjectTypeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.InterfaceObject);
    }

    /// <summary>
    /// Applies the @key directive which is used to indicate a combination of fields that can be used to uniquely
    /// identify and fetch an object or interface. The specified field set can represent single field (e.g. "id"),
    /// multiple fields (e.g. "id name") or nested selection sets (e.g. "id user { name }"). Multiple keys can
    /// be specified on a target type.
    ///
    /// Keys can be marked as non-resolvable which indicates to router that given entity should never be
    /// resolved within given subgraph. This allows your subgraph to still reference target entity without
    /// contributing any fields to it.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   field: String
    ///   bars: [Bar!]!
    /// }
    ///
    /// type Bar @key(fields: "id", resolvable: false) {
    ///   id: ID!
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <param name="resolvable">
    /// Boolean flag to indicate whether this entity is resolvable locally.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEntityResolverDescriptor Key(
        this IObjectTypeDescriptor descriptor,
        string fieldSet,
        bool? resolvable = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Key_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)),
        };
        if (false == resolvable)
        {
            arguments.Add(
                new ArgumentNode(
                    WellKnownArgumentNames.Resolvable,
                    new BooleanValueNode(false))
            );
        }

        descriptor.Directive(
            WellKnownTypeNames.Key,
            arguments.ToArray());

        return new EntityResolverDescriptor<object>(descriptor);
    }

    /// <inheritdoc cref="Key(IObjectTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor Key(
        this IInterfaceTypeDescriptor descriptor,
        string fieldSet,
        bool? resolvable = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Key_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)),
        };
        if (false == resolvable)
        {
            arguments.Add(
                new ArgumentNode(
                    WellKnownArgumentNames.Resolvable,
                    new BooleanValueNode(false))
            );
        }

        return descriptor.Directive(
            WellKnownTypeNames.Key,
            arguments.ToArray());
    }

    /// <summary>
    /// Applies @link directive definitions to link the document to external schemas.
    /// External schemas are identified by their url, which optionally ends with a name and version with
    /// the following format: `{NAME}/v{MAJOR}.{MINOR}`
    ///
    /// By default, external types should be namespaced (prefixed with namespace__, e.g. key directive
    /// should be namespaced as federation__key) unless they are explicitly imported. We automatically
    /// import ALL federation directives to avoid the need for namespacing.
    ///
    /// NOTE: We currently DO NOT support full @link directive capability as it requires support for
    /// namespacing and renaming imports. This functionality may be added in the future releases.
    /// See @link specification for details.

    /// <example>
    /// extend schema @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
    ///
    /// type Query {
    ///   foo: Foo!
    /// }
    ///
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   name: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="import">
    /// Optional list of imported elements.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor Link(
        this ISchemaTypeDescriptor descriptor,
        string url,
        string?[]? import)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        return descriptor.Directive(new LinkDirective(url, import?.ToList()));
    }

    /// <summary>
    /// Applies the @override directive which is used to indicate that the current subgraph is taking
    /// responsibility for resolving the marked field away from the subgraph specified in the from
    /// argument. Name of the subgraph to be overridden has to match the name of the subgraph that
    /// was used to publish their schema.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   description: String @override(from: "BarSubgraph")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="from">
    /// Name of the subgraph to be overridden.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="from"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Override(
        this IObjectFieldDescriptor descriptor,
        string from)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(from))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Override_From_CannotBeNullOrEmpty,
                nameof(from));
        }

        return descriptor.Directive(
            WellKnownTypeNames.Override,
            new ArgumentNode(
                WellKnownArgumentNames.From,
                new StringValueNode(from)));
    }

    /// <summary>
    /// Applies the @provides directive which is a router optimization hint specifying field set that
    /// can be resolved locally at the given subgraph through this particular query path. This
    /// allows you to expose only a subset of fields from the underlying entity type to be selectable
    /// from the federated schema without the need to call other subgraphs. Provided fields specified
    /// in the directive field set should correspond to a valid field on the underlying GraphQL
    /// interface/object type. @provides directive can only be used on fields returning entities.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///     id: ID!
    ///     # implies name field can be resolved locally
    ///     bar: Bar @provides(fields: "name")
    ///     # name fields are external
    ///     # so will be fetched from other subgraphs
    ///     bars: [Bar]
    /// }
    ///
    /// type Bar @key(fields: "id") {
    ///     id: ID!
    ///     name: String @external
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The fields that are guaranteed to be selectable by the gateway.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Provides(
        this IObjectFieldDescriptor descriptor,
        string fieldSet)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Provides_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        return descriptor.Directive(
            WellKnownTypeNames.Provides,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)));
    }

    /// <summary>
    /// Applies the @requires directive which is used to specify external (provided by other subgraphs)
    /// entity fields that are needed to resolve target field. It is used to develop a query plan where
    /// the required fields may not be needed by the client, but the service may need additional
    /// information from other subgraphs. Required fields specified in the directive field set should
    /// correspond to a valid field on the underlying GraphQL interface/object and should be instrumented
    /// with @external directive.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   # this field will be resolved from other subgraph
    ///   remote: String @external
    ///   local: String @requires(fields: "remote")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The <paramref name="fieldSet"/> describes which fields may
    /// not be needed by the client, but are required by
    /// this service as additional information from other services.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Requires(
        this IObjectFieldDescriptor descriptor,
        string fieldSet)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Requires_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        return descriptor.Directive(
            WellKnownTypeNames.Requires,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)));
    }

    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple subgraphs.
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
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Shareable(this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.Shareable);
    }

    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple subgraphs.
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
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Shareable(this IObjectTypeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.Shareable);
    }
        /// <summary>
    /// Applies @extends directive which is used to represent type extensions in the schema. Federated extended types should have
    /// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
    ///
    /// NOTE: Federation v2 no longer requires `@extends` directive due to the smart entity type merging. All usage of @extends
    /// directive should be removed from your Federation v2 schemas.
    /// <example>
    /// # extended from the Users service
    /// type Foo @extends @key(fields: "id") {
    ///   id: ID
    ///   description: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> ExtendsType<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.Extends);
    }

    /// <summary>
    /// Adds the @key directive which is used to indicate a combination of fields that can be used to uniquely
    /// identify and fetch an object or interface. The specified field set can represent single field (e.g. "id"),
    /// multiple fields (e.g. "id name") or nested selection sets (e.g. "id user { name }"). Multiple keys can
    /// be specified on a target type.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   field: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEntityResolverDescriptor<T> Key<T>(
        this IObjectTypeDescriptor<T> descriptor,
        string fieldSet)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Key_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        descriptor.Directive(
            WellKnownTypeNames.Key,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)));

        return new EntityResolverDescriptor<T>(descriptor);
    }

    /// <summary>
    /// Mark the type as an extension
    /// of a type that is defined by another service when
    /// using apollo federation.
    /// </summary>
    [Obsolete("Use ExtendsType type instead")]
    public static IObjectTypeDescriptor<T> ExtendServiceType<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCreate(d => d.ContextData[ExtendMarker] = true);

        return descriptor;
    }

    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> InterfaceObject<T>(this IObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.InterfaceObject);
    }

    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple subgraphs.
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
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> Shareable<T>(this IObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownTypeNames.Shareable);
    }
}
