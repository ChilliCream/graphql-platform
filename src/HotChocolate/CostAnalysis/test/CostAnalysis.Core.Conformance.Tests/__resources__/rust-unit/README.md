# Rust estimator unit fixtures

This directory contains 32 ExactCase JSON fixtures ported from the
`graphql-static-analysis-rs` cost estimator unit tests. Each fixture names
its source test and line in `src/analyses/cost/estimator.rs`.

The family exercises default and signed type costs, abstract output costs,
field collection, list-size assumptions and slicing arguments, inherited
`sizedFields`, input object coercion and costs, directive argument costs,
and operation and schema defaults.

The source module contains 40 tests. These eight tests have no fixture in
this directory:

- `variables_are_an_explicit_estimate_input`, API shape
- `abstract_type_uses_the_maximum_possible_object_weight`, private model API
- `negative_type_weight_is_preserved`, private model API
- `syntactic_analysis_preserves_signed_type_costs`, Syntactic backend
- `syntactic_negative_type_cost_does_not_fall_back_to_exact_cases`, Syntactic backend
- `operation_variable_default_prunes_syntactic_cost`, Syntactic backend
- `syntactic_type_products_merge_response_names_across_redundant_types`, Syntactic backend
- `estimator_can_be_reused_across_operations`, API shape

## Attribution

Source: [graphql-static-analysis-rs](https://github.com/duckki/graphql-static-analysis-rs),
`src/analyses/cost/estimator.rs` lines 603-1464 at commit
`fec57fd7a980b5399637fa464a9bdce0781d91dd` (v0.2.0).

Copyright 2026 graphql-static-analysis contributors. Licensed under the
MIT License. The repository's `__resources__/NOTICE.md` records the license
and source provenance.

No fixture in this directory is copied from the unlicensed
`graphql-lean` repository.
