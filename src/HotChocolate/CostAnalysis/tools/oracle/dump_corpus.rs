//! Dumps the ExactCase cost-observation rows of the exhaustive TreeSummary
//! matrix as HotChocolate CostAnalysis conformance fixtures, plus a manifest
//! describing the corpus shape.
//!
//! Usage: cargo run --example dump_corpus -- OUTPUT_DIRECTORY
//!
//! Writes `OUTPUT_DIRECTORY/exact-case.json` (an array of fixtures matching
//! `fixture.schema.json`) and `OUTPUT_DIRECTORY/manifest.json`.

use apollo_compiler::request::coerce_variable_values;
use apollo_compiler::ExecutableDocument;
use apollo_compiler::Schema;
use graphql_static_analysis::cost::CostEstimator;
use graphql_static_analysis::cost::CostModel;
use graphql_static_analysis::AnalysisMode;
use graphql_static_analysis_fuzz::tree_summary::{schema, variables_json, TreeSummaryInput};
use serde::Serialize;
use std::collections::BTreeSet;
use std::env;
use std::fs;
use std::path::PathBuf;

/// The one line dropped from the shared fuzz schema: it collides with
/// HotChocolate's built-in `@tag` directive and no dumped operation uses it.
const TAG_DIRECTIVE_LINE: &str = "directive @tag(flag: Boolean) on FIELD | INLINE_FRAGMENT";

const SOURCE: &str = "rust-corpus";
const BACKEND: &str = "exact-case";
const PINNED_COMMIT: &str = "fec57fd7a980b5399637fa464a9bdce0781d91dd";

#[derive(Serialize)]
struct Fixture {
    id: String,
    source: &'static str,
    sdl: String,
    operation: String,
    #[serde(rename = "operationName")]
    operation_name: String,
    variables: serde_json::Value,
    #[serde(rename = "defaultListSize")]
    default_list_size: f64,
    backend: &'static str,
    expected: ExpectedCost,
    notes: String,
}

#[derive(Serialize)]
struct ExpectedCost {
    #[serde(rename = "typeCost")]
    type_cost: f64,
    #[serde(rename = "fieldCost")]
    field_cost: f64,
}

#[derive(Serialize)]
struct Manifest {
    #[serde(rename = "matrixSize")]
    matrix_size: usize,
    #[serde(rename = "exactCostRows")]
    exact_cost_rows: usize,
    emitted: usize,
    #[serde(rename = "excludedInvalidVariables")]
    excluded_invalid_variables: usize,
    deduplicated: usize,
    #[serde(rename = "sourceCommit")]
    source_commit: &'static str,
}

fn main() {
    let output_dir = env::args_os()
        .nth(1)
        .map(PathBuf::from)
        .expect("usage: dump_corpus OUTPUT_DIRECTORY");
    fs::create_dir_all(&output_dir).expect("create output directory");

    let sdl = schema()
        .lines()
        .filter(|line| line.trim() != TAG_DIRECTIVE_LINE)
        .collect::<Vec<_>>()
        .join("\n");

    let matrix_size = TreeSummaryInput::exhaustive_cases().count();

    let mut fixtures = Vec::new();
    let mut seen_requests = BTreeSet::new();
    let mut exact_cost_rows = 0usize;
    let mut excluded_invalid_variables = 0usize;
    let mut deduplicated = 0usize;

    for input in TreeSummaryInput::exhaustive_cases() {
        // Only the IBM cost observation (3) on the ExactCase backend (mode 0)
        // belongs in this corpus; Syntactic-backend rows are not emitted.
        if input.mode != 0 || input.observation != 3 {
            continue;
        }
        exact_cost_rows += 1;

        let schema_valid = Schema::parse_and_validate(schema(), "schema.graphql")
            .expect("shared fuzz schema is valid");
        let query = input.query();
        let document_valid =
            ExecutableDocument::parse_and_validate(&schema_valid, &query, "case.graphql")
                .unwrap_or_else(|errors| {
                    panic!("shared fuzz operation is valid: {query}\n{errors:?}")
                });
        let operation_for_coercion = document_valid
            .operations
            .get(Some("Case"))
            .expect("Case operation");

        let raw_variables = variables_json(input.variable_case).unwrap_or_default();
        let coerced =
            match coerce_variable_values(&schema_valid, operation_for_coercion, &raw_variables) {
                Ok(coerced) => coerced,
                Err(_) => {
                    // Bypasses GraphQL coercion by construction (R-CORPUS-VARIABLES);
                    // a case real coercion would reject never reaches the .NET
                    // engine's coerced-variables input, so it is dropped here.
                    excluded_invalid_variables += 1;
                    continue;
                }
            };

        let variables_value =
            serde_json::to_value(&raw_variables).expect("variables serialize to JSON");
        let dedup_key = (query.clone(), variables_value.to_string(), input.list_size);
        if !seen_requests.insert(dedup_key) {
            // variable_case 0 (no variables) and 1 (an empty variables map)
            // reach the estimator as the same coerced request.
            deduplicated += 1;
            continue;
        }

        let schema_plain = schema_valid.into_inner();
        let document_plain = document_valid.into_inner();
        let operation = document_plain
            .operations
            .get(Some("Case"))
            .expect("Case operation");

        let model = CostModel::from_schema(&schema_plain).expect("cost model from shared schema");
        let cost = CostEstimator::new(model)
            .mode(AnalysisMode::ExactCase)
            .default_list_size(u64::from(input.list_size))
            .estimate(&document_plain, operation, &coerced)
            .expect("IBM cost analysis");

        fixtures.push(Fixture {
            id: format!("exact-case-{}", input.request_id()),
            source: SOURCE,
            sdl: sdl.clone(),
            operation: query,
            operation_name: "Case".to_string(),
            variables: variables_value,
            default_list_size: f64::from(input.list_size),
            backend: BACKEND,
            expected: ExpectedCost {
                type_cost: cost.type_cost,
                field_cost: cost.field_cost,
            },
            notes: format!(
                "family={} variable_case={} list_size={} default_case={} structural={} {}",
                input.family,
                input.variable_case,
                input.list_size,
                input.default_case,
                input.structural,
                input.reproduction(),
            ),
        });
    }

    let emitted = fixtures.len();
    assert_eq!(
        emitted + excluded_invalid_variables + deduplicated,
        exact_cost_rows,
        "every ExactCase cost row must be emitted, excluded, or deduplicated exactly once"
    );

    let fixtures_json = serde_json::to_string_pretty(&fixtures).expect("serialize fixtures");
    fs::write(
        output_dir.join("exact-case.json"),
        format!("{fixtures_json}\n"),
    )
    .expect("write exact-case.json");

    let manifest = Manifest {
        matrix_size,
        exact_cost_rows,
        emitted,
        excluded_invalid_variables,
        deduplicated,
        source_commit: PINNED_COMMIT,
    };
    let manifest_json = serde_json::to_string_pretty(&manifest).expect("serialize manifest");
    fs::write(
        output_dir.join("manifest.json"),
        format!("{manifest_json}\n"),
    )
    .expect("write manifest.json");

    println!(
        "matrix={matrix_size} exact-cost-rows={exact_cost_rows} emitted={emitted} \
         excluded-invalid-variables={excluded_invalid_variables} deduplicated={deduplicated}"
    );
}
