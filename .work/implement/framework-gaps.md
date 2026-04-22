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

