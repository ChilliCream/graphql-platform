# Apollo Federation compliance: framework gaps

This log captures framework gaps that block individual compliance suites in
`src/HotChocolate/Fusion/test/Fusion.Connectors.ApolloFederation.Compliance.Tests/`.
Each entry lists the suite, the failing scenario, the location of the offending
production code, and a one-paragraph fix sketch.

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

