# Nightly fuzz disagreements

The live Rust oracle writes a standalone fixture here when a generated case
does not match the .NET engine bit-for-bit. The fixture records the seed and
case identifier, uses `source: "fuzz-found"`, and stores the oracle result as
the expected cost.

The generator produces independent small cases, so minimization first isolates
the disagreement to one schema, operation, variables object, and default list
size. Copy the workflow artifact into this directory before fixing the engine.
The fixture must stay vendored after the fix so the conformance theory guards
the regression without Rust.

Do not promote cases containing introspection meta-fields, `@defer`, or
`@stream`. Those constructs are outside the bit-exact agreement scope and the
generator rejects them before emission.
