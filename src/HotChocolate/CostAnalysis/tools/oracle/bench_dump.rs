//! Dumps the pinned crate's generated cost benchmark scenarios for the
//! HotChocolate head-to-head runner.

use super::{
    expected_cost, operation_source, pathological_boolean_scenario, scenario,
    schema_sdl_with_topology, Backend, Scenario, ABSTRACT_TYPE_COUNT,
};
use graphql_static_analysis::cost::{CostEstimator, CostModel};
use graphql_static_analysis::AnalysisMode;
use serde::Serialize;
use std::fs;
use std::path::Path;

const INCIDENCES_PER_OBJECT: usize = 4;
const PATHOLOGICAL_BOOLEAN_MAX_K: usize = 6;

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct BenchmarkCorpus {
    source_commit: &'static str,
    default_list_size: u64,
    scenarios: Vec<BenchmarkScenario>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct BenchmarkScenario {
    id: String,
    axis: &'static str,
    object_types: usize,
    abstract_types: usize,
    incidences_per_object: usize,
    query_spreads: usize,
    boolean_variables_per_region: Option<usize>,
    schema: String,
    operation: String,
    variables: serde_json::Value,
    expected_type_cost: f64,
    expected_field_cost: f64,
}

pub(super) fn run(output_path: &str) {
    let mut scenarios = Vec::new();

    for (object_types, query_spreads) in [(1_024, 8), (10_240, 8), (1_024, 80)] {
        scenarios.push(topology_scenario(
            "cost-endpoints",
            object_types,
            query_spreads,
        ));
    }

    for scale in 1..=10 {
        scenarios.push(topology_scenario("cost-schema-size", scale * 1_024, 8));
    }

    for scale in 1..=10 {
        scenarios.push(topology_scenario("cost-query-size", 1_024, scale * 8));
    }

    for variable_count in 1..=PATHOLOGICAL_BOOLEAN_MAX_K {
        scenarios.push(pathological_scenario(variable_count));
    }

    let corpus = BenchmarkCorpus {
        source_commit: "fec57fd7a980b5399637fa464a9bdce0781d91dd",
        default_list_size: 1,
        scenarios,
    };
    let json = serde_json::to_string_pretty(&corpus).expect("serialize benchmark corpus");
    let output_path = Path::new(output_path);
    if let Some(parent) = output_path.parent() {
        fs::create_dir_all(parent).expect("create benchmark corpus directory");
    }
    fs::write(output_path, format!("{json}\n")).expect("write benchmark corpus");
}

fn topology_scenario(
    axis: &'static str,
    object_types: usize,
    query_spreads: usize,
) -> BenchmarkScenario {
    let source = scenario(object_types, query_spreads);
    let expected = estimate_cost(&source);
    assert_eq!(expected, expected_cost(Backend::ExactCase));

    BenchmarkScenario {
        id: format!("{axis}-{object_types}-{query_spreads}"),
        axis,
        object_types,
        abstract_types: ABSTRACT_TYPE_COUNT,
        incidences_per_object: INCIDENCES_PER_OBJECT,
        query_spreads,
        boolean_variables_per_region: None,
        schema: schema_sdl_with_topology(object_types, ABSTRACT_TYPE_COUNT, INCIDENCES_PER_OBJECT),
        operation: operation_source(query_spreads),
        variables: serde_json::to_value(source.variables).expect("serialize variables"),
        expected_type_cost: expected.0,
        expected_field_cost: expected.1,
    }
}

fn pathological_scenario(variable_count: usize) -> BenchmarkScenario {
    let source = pathological_boolean_scenario(variable_count);
    let expected = estimate_cost(&source);

    BenchmarkScenario {
        id: format!("pathological-booleans-{variable_count}"),
        axis: "pathological-booleans",
        object_types: 2,
        abstract_types: 1,
        incidences_per_object: 0,
        query_spreads: 2 * variable_count,
        boolean_variables_per_region: Some(variable_count),
        schema: source.schema.to_string(),
        operation: source.document.to_string(),
        variables: serde_json::to_value(source.variables).expect("serialize variables"),
        expected_type_cost: expected.0,
        expected_field_cost: expected.1,
    }
}

fn estimate_cost(scenario: &Scenario) -> (f64, f64) {
    let estimator = CostEstimator::new(
        CostModel::from_schema(&scenario.schema).expect("create benchmark cost model"),
    )
    .mode(AnalysisMode::ExactCase)
    .default_list_size(1);
    super::estimate_cost(scenario, &estimator)
}
