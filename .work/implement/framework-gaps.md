# Apollo Federation compliance: framework gaps

This log captures framework gaps that block individual compliance suites in
`src/HotChocolate/Fusion/test/Fusion.Connectors.ApolloFederation.Compliance.Tests/`.
Each entry lists the suite, the failing scenario, the location of the offending
production code, and a one-paragraph fix sketch.

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

