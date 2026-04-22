# Apollo Federation compliance: framework gaps

This log captures framework gaps that block individual compliance suites in
`src/HotChocolate/Fusion/test/Fusion.Connectors.ApolloFederation.Compliance.Tests/`.
Each entry lists the suite, the failing scenario, the location of the offending
production code, and a one-paragraph fix sketch.

## enum-intersection (partial)

- File: `src/HotChocolate/Fusion/src/Fusion.Execution/` (response shaping for enum values).
- Repro suite: `Suites/EnumIntersection/EnumIntersectionTests.cs::UsersB_Type_Returns_Null_For_Inaccessible_Value` and `Suites/EnumIntersection/EnumIntersectionTests.cs::Users_Type_Returns_Null_For_Subgraph_A_Side`.
- Rule that is wrong: when the supergraph excludes an enum value (because it is `@inaccessible` or because it is missing from one source schema's enum), the gateway should null out the field and emit an error if the source subgraph returns the excluded value. Today the gateway forwards the source value as-is.
- Expected behavior: after composing the supergraph enum, the gateway should validate the values returned by source subgraphs against the public enum set. Excluded values become `null` (with an error attached when the field is non-null) and never leak through to clients.
- Fix sketch: in the result shaping code that converts subgraph enum values into the gateway response, look up each value against the supergraph enum's public values. When the value is missing or inaccessible, emit a field error and produce `null`.

## mutations (partial)

- File: `src/HotChocolate/Fusion/src/Fusion/Planning/` planner; specifically the absence of `@requires` field projection through the entity lookup.
- Repro suite: `Suites/Mutations/MutationsTests.cs::AddProduct_Composes_From_Two_Subgraphs` and `Suites/Mutations/MutationsTests.cs::Product_Composes_From_Two_Subgraphs`.
- Rule that is wrong: when subgraph `b` declares `isExpensive: Boolean! @requires(fields: "price")`, the resolver in `b` expects the parent <c>Product</c> to carry a populated `price` value. The planner should fetch `price` from the subgraph that owns it (subgraph `a`) and attach it to the entity representation passed to `b._entities`. Today the resolver runs without `price`, throws, and the gateway surfaces "Unexpected Execution Error".
- Expected behavior: the planner identifies fields with `@requires` annotation, fetches their dependency selection from any subgraph that can produce the dependency, and includes those fields in the entity representation handed to the lookup. The downstream `__resolveReference` then receives the dependency in its key argument or via the federation external setter.
- Fix sketch: in the planner, before emitting the entity lookup call to the subgraph hosting a `@requires` field, plan a sibling fetch for the required dependency from a subgraph that owns it, then enrich the lookup representation with the resolved value. The Apollo Federation runtime adapter already exposes the `ExternalSetter` mechanism that would populate the field on the resolved entity. This is the Phase C bucket work.

## shared-root (partial)

- File: `src/HotChocolate/Fusion/src/Fusion/Planning/` query planner; specifically the absence of cross-subgraph list zipping for shareable non-entity types.
- Repro suite: `Suites/SharedRoot/SharedRootTests.cs::Products_Composes_Fields_From_Three_Subgraphs`.
- Rule that is wrong: when a non-keyed object type is reachable through the same shareable list root field in multiple subgraphs, the planner picks a single subgraph for the list and emits `null` for fields that subgraph cannot resolve. The expectation in Apollo's reference is to issue parallel root queries, then merge the lists element-by-element by position.
- Expected behavior: for shareable list roots over a non-entity type, the planner should fan out to all subgraphs that can produce at least one needed field, then zip the parallel result lists by index. The single-product variant already works because there is only one element to merge.
- Fix sketch: extend the planner to recognize shareable root selections returning list-of-non-entity-types and emit a parallel-fanout, zip-by-index merge plan. The merge step needs no key, only an index alignment guarantee from each subgraph's resolver. This is a planner enhancement and is out of scope for the test enablement pass.

## typename

- File: `src/HotChocolate/Fusion/src/Fusion.Composition.ApolloFederation/` (composer adapter), specifically the absence of `@interfaceObject` translation.
- Repro suite: `Suites/Typename/TypenameTests.cs` (six audit cases).
- Rule that is wrong: subgraph `b` uses `type User @key(fields: "id") @interfaceObject` to abstractly extend an interface defined in subgraph `a`. Without an `@interfaceObject` translation in the Apollo Federation adapter the composer either rejects the SDL or fails to recognize the subgraph as contributing fields to all concrete implementations.
- Expected behavior: composing across `@interfaceObject` should treat `type User @interfaceObject` as a contribution to every concrete type that implements `interface User` in any other subgraph. Apollo Federation's composer accepts this and the runtime honors `__typename` from the source schema that owns the concrete type (in this audit, subgraph `a`).
- Fix sketch: extend the federation transformer in `Fusion.Composition.ApolloFederation` to recognize `@interfaceObject` and emit field contributions onto every concrete implementer of the interface declared in another subgraph. Mirror Apollo's behavior: forward `name` requests on each implementer to the `@interfaceObject` source schema via the entity lookup keyed on the interface key fields. This is the Phase E `@interfaceObject` work and is intentionally out of scope for the Phase A enablement pass.

## parent-entity-call

- File: `src/HotChocolate/Fusion/src/Fusion.Composition/SatisfiabilityValidator.cs`
- Symptom: composition fails with `Cycle detected in requirement: A:Category.id<ID> -> B:Category.id<ID> -> A:Category.id<ID>`.
- Repro suite: `Suites/ParentEntityCall/ParentEntityCallTests.cs::Products_Resolves_Category_Details_From_Parent_Entity_Call`.
- Rule that is wrong: when satisfying field access on a keyed entity, the
  validator recursively requires the key fields through the entity lookup,
  even though the key fields are intrinsically present on any instance of
  the entity type. With two subgraphs declaring the same `@key` on `Category`
  and only being reachable via that lookup, the satisfiability search cycles
  between the two subgraphs.
- Expected behavior: when computing satisfiability for a field on a keyed
  entity, the validator should treat the entity's key fields as already
  available on the entity instance (they are by definition reachable through
  the lookup that produced the instance) and stop expanding requirements for
  them. Apollo's composer admits this composition.
- Fix sketch: in the satisfiability search, before resolving a field
  requirement on a keyed entity type, short-circuit the lookup for fields
  that are part of any `@key` on the same type in the current source schema.
  Track that the cycle root and the cycle target are both key fields of the
  same lookup and treat them as satisfied trivially. This mirrors the
  composite schema spec behavior that key fields are always projected by the
  lookup.

## keys-mashup (partial)

- File: `src/HotChocolate/Fusion/src/Fusion/Planning/` query planner; the same `@requires` projection gap surfaced by the mutations and include-skip suites.
- Repro suite: `Suites/KeysMashup/KeysMashupTests.cs::B_Resolves_A_Name_And_NameInB_Via_Requires`.
- Rule that is wrong: subgraph `b` declares `nameInB: String! @requires(fields: "name")` on the `A` entity. The planner should fetch `name` from subgraph `a` (where it lives) and attach it to the entity representation passed to subgraph `b`'s entity lookup so the `nameInB` resolver receives a populated `name`. Today the resolver runs without `name`, throws, and the gateway surfaces "Unexpected Execution Error".
- Expected behavior: the planner identifies fields with `@requires` annotations and routes the dependency selection through the entity lookup to the subgraph that owns the dependency, then enriches the lookup representation with the resolved value before invoking the downstream `__resolveReference`.
- Fix sketch: same Phase C work item as the mutations entry above. Once the planner threads `@requires` dependencies through the lookup, this case unblocks without changes here.

## fed2-external-extends

- File: `src/HotChocolate/Fusion/src/Fusion.Composition.ApolloFederation/GenerateLookupFields.cs` line 105.
- Repro suite: every test in `Suites/Fed2ExternalExtends/Fed2ExternalExtendsTests.cs` (four cases).
- Rule that is wrong: the lookup field generator builds a name like `userById` from `User @key(fields: "id")` (camel-case type plus `By` plus pascal-case argument). When the source subgraph already exposes a user-declared `Query.userById(id: ID): User` field, `Schema.QueryType.Fields.Add` throws `An item with the same key has already been added. Key: userById` and the entire composition fails. The audit's subgraph `b` exposes exactly that user-facing root field together with the resolvable `User @key("id")`, so the suite cannot compose at all.
- Expected behavior: the transformer should detect the collision and either reuse the existing root field (if it already returns the same entity by the same key) or pick a non-colliding generated name (for example, prefix with an internal marker) so the composition continues. Apollo Federation tolerates this overlap because it routes entity calls through the synthetic `_entities(representations: [_Any!]!)` field, not through the user-declared root field.
- Fix sketch: in `GenerateLookupFields.Apply`, before calling `schema.QueryType.Fields.Add`, check whether a field with that name is already present. If so, either skip the generated lookup (the user-declared field can serve as the entity gateway) or emit it under a mangled name with the `@internal @lookup` directives still in place. The latter keeps the connector wired to a guaranteed-internal field and avoids tripping the field collection.

## fed2-external-extension

- File: same as fed2-external-extends, `src/HotChocolate/Fusion/src/Fusion.Composition.ApolloFederation/GenerateLookupFields.cs` line 105.
- Repro suite: every test in `Suites/Fed2ExternalExtension/Fed2ExternalExtensionTests.cs` (four cases).
- Rule that is wrong: identical collision as fed2-external-extends. The audit's subgraph `b` exposes `Query.userById(id: ID): User` together with `User @key(fields: "id")`. The only difference between this suite and fed2-external-extends is that subgraph `a` declares `extend type User @key(...)` (the v2 form) instead of `type User @key(...) @extends` (the v1-style form). Both forms exercise the same composer path and hit the same name collision in the lookup field generator.
- Expected behavior: same as fed2-external-extends.
- Fix sketch: same as fed2-external-extends. The same fix unblocks both suites in lockstep.

## null-keys (flaky)

- File: planner / runtime entity-call path under `src/HotChocolate/Fusion/src/Fusion.Execution/`.
- Repro suite: `Suites/NullKeys/NullKeysTests.cs::BookContainers_Resolves_Null_Author_When_Bridge_Subgraph_Returns_Null` (intermittent: about one in five runs fails with all three book authors null instead of just the third).
- Rule that is wrong: the three-subgraph chain `a -> b -> c` should produce `[{author: Alice}, {author: Bob}, {author: null}]`. The third book's `author` becomes `null` because subgraph `b`'s reference resolver returns `null` for the bridge book with `id == "3"`. When the run fails, all three authors come back `null`, suggesting the planner mis-aligns the `_entities` response from subgraph `b` against the `bookContainers` list (or, when one element of `_entities` is null, the runtime drops the entity-call results for the surviving siblings).
- Expected behavior: the planner must zip `_entities` results back to the originating list positions stably. A null entry in the `_entities` response should leave the corresponding slot null without affecting the other slots' downstream entity calls.
- Fix sketch: audit the slot-matching code in the entity-call result merger. Verify that when an entity-call response contains a null at index `i`, the merger skips downstream calls for slot `i` only and continues to issue downstream calls for slots `j != i`. A reproducer that runs the suite under a fixed RNG seed (or a single planner thread) would help isolate the race.

## simple-requires-provides (provides external-field removal)

- File: `src/HotChocolate/Fusion/src/Fusion.Composition.ApolloFederation/RemoveExternalFields.cs` line 16.
- Repro suite: every test in `Suites/SimpleRequiresProvides/SimpleRequiresProvidesTests.cs` (twelve cases). Composition fails up front with: `Source schema validation failed. The @provides directive on field 'Review.author' in schema 'reviews' specifies an invalid field selection. - errors: The field 'username' does not exist on the type 'User'.`
- Rule that is wrong: the federation transformer unconditionally strips every `@external` field from each subgraph after running the rest of the transformations. The Composite Schema Spec composer that runs next still expects to see those fields (in particular, the spec's `ProvidesInvalidFieldsRule` walks the `@provides` selection set against the local schema and the `ProvidesFieldsMissingExternalRule` requires the targeted field to carry the `@external` marker). Removing the field before validation makes every `@provides` selection point at a non-existent field and the source-schema validation rejects the composition before any test can run.
- Expected behavior: external fields referenced by a `@provides(fields: ...)` selection on the same subgraph must remain on their owning type with the `@external` marker intact, so the composer can validate the `@provides` selection set and the runtime can route the provided fields back through the source schema. Apollo Federation accepts this composition.
- Fix sketch: in `RemoveExternalFields.Apply`, before deleting an external field, check whether any sibling field on the same subgraph carries a `@provides` (or `@requires`) selection that references the field, directly or through a nested path. When a reference exists, leave the field in place with its `@external` directive; only remove unreferenced external fields. Equivalently, run a precomputation pass that walks every `@provides` and `@requires` selection on the schema and marks the referenced fields as "must keep", then have the removal pass skip marked fields. Either approach keeps the spec composer and the runtime in sync without changing how `@requires` arguments are emitted.

## simple-requires-provides (require argument forced to non-null)

- File: `src/HotChocolate/Fusion/src/Fusion.Composition.ApolloFederation/TransformRequiresToRequire.cs` line 118.
- Repro suite: same twelve cases in `Suites/SimpleRequiresProvides/SimpleRequiresProvidesTests.cs`. Surfaces only after the `@provides` removal gap is worked around. Composition fails with: `Post-merge validation failed. The @require directive on argument 'Product.shippingEstimate(price:)' in schema 'inventory' specifies an invalid field selection against the composed schema. - errors: The field 'price' is of type 'Int' instead of the expected input type 'Int!'.`
- Rule that is wrong: when transforming `@requires(fields: "price weight")` on the `inventory` subgraph into `@require` arguments, the transformer always wraps the argument's input type in `NonNull` via `EnsureNonNull(StripNonNull(fieldType))`, regardless of whether the source field on the owning subgraph is nullable. The audit's `products` subgraph declares `price: Int` and `weight: Int` (both nullable), so the post-merge validator compares the composed schema's `Int` against the require argument's `Int!` and rejects the composition.
- Expected behavior: the require argument's input type should mirror the nullability of the source field. When the field is nullable on its owning subgraph, the generated `@require` argument should also be nullable. Apollo Federation accepts the audit composition because its lookup payload allows a missing key field, and the require resolver is responsible for handling the absence.
- Fix sketch: in `TransformRequiresToRequire.ExtractRequireArguments`, drop the unconditional `EnsureNonNull(StripNonNull(fieldType))` wrapping. Use the source field's type as-is when it implements `IInputType`, falling back to the wrapped form only when the source field is non-null. The downstream require validator already treats nullable arguments as optional inputs, so this mirrors the spec without further changes.

## simple-requires-provides (planner @requires routing)

- Repro suite: `Suites/SimpleRequiresProvides/SimpleRequiresProvidesTests.cs` (the seven cases that select `shippingEstimate` and/or `shippingEstimateTag`). Currently masked by the two composition-time gaps above; once those are fixed, these tests will hit the same Phase C planner gap as `mutations` and `keys-mashup`. See the `mutations (partial)` entry above for the rule and the fix sketch.

