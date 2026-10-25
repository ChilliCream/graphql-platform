# Nightly fuzz disagreements

The live Rust oracle writes a standalone fixture here when a generated case
does not match the .NET implementation bit-for-bit. The fixture records the
seed and case identifier, uses `source: "fuzz-found"`, and stores the oracle
result as the expected cost.

Each fixture contains one schema, operation, variables object, and default list
size. Copy the workflow artifact into this directory to include it in the
conformance suite. These fixtures run without Rust.
