//! Prices generated cases at the graphql-static-analysis request boundary.

use apollo_compiler::request::coerce_variable_values;
use apollo_compiler::response::JsonMap;
use apollo_compiler::{ExecutableDocument, Schema};
use graphql_static_analysis::cost::{CostEstimator, CostModel};
use graphql_static_analysis::AnalysisMode;
use serde::{Deserialize, Serialize};
use std::env;
use std::fs::{self, File};
use std::io::{BufWriter, Write};

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct Case {
    id: String,
    sdl: String,
    oracle_operation: String,
    operation_name: String,
    variables: serde_json::Value,
    default_list_size: u64,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct ResultRow {
    id: String,
    type_cost: f64,
    field_cost: f64,
    type_cost_bits: String,
    field_cost_bits: String,
}

fn main() {
    let (input, output) = arguments();
    let cases: Vec<Case> =
        serde_json::from_str(&fs::read_to_string(input).expect("read generated cases"))
            .expect("parse generated cases");
    let mut writer = BufWriter::new(File::create(output).expect("create oracle results"));

    for case in cases {
        let schema_valid = Schema::parse_and_validate(&case.sdl, "schema.graphql")
            .unwrap_or_else(|errors| panic!("{} schema is valid: {errors:?}", case.id));
        let document_valid = ExecutableDocument::parse_and_validate(
            &schema_valid,
            &case.oracle_operation,
            "case.graphql",
        )
        .unwrap_or_else(|errors| panic!("{} operation is valid: {errors:?}", case.id));
        let operation_for_coercion = document_valid
            .operations
            .get(Some(&case.operation_name))
            .unwrap_or_else(|_| panic!("{} operation exists", case.id));
        let variables: JsonMap = serde_json::from_value(case.variables)
            .unwrap_or_else(|error| panic!("{} variables are an object: {error}", case.id));
        let coerced = coerce_variable_values(&schema_valid, operation_for_coercion, &variables)
            .unwrap_or_else(|errors| panic!("{} variables coerce: {errors:?}", case.id));

        let schema = schema_valid.into_inner();
        let document = document_valid.into_inner();
        let operation = document
            .operations
            .get(Some(&case.operation_name))
            .unwrap_or_else(|_| panic!("{} operation exists", case.id));
        let model = CostModel::from_schema(&schema)
            .unwrap_or_else(|error| panic!("{} cost model builds: {error}", case.id));
        let cost = CostEstimator::new(model)
            .mode(AnalysisMode::ExactCase)
            .default_list_size(case.default_list_size)
            .estimate(&document, operation, &coerced)
            .unwrap_or_else(|error| panic!("{} cost estimate succeeds: {error}", case.id));
        let row = ResultRow {
            id: case.id,
            type_cost: cost.type_cost,
            field_cost: cost.field_cost,
            type_cost_bits: format!("{:016x}", cost.type_cost.to_bits()),
            field_cost_bits: format!("{:016x}", cost.field_cost.to_bits()),
        };
        serde_json::to_writer(&mut writer, &row).expect("serialize oracle result");
        writer.write_all(b"\n").expect("write oracle result line");
    }
}

fn arguments() -> (String, String) {
    let mut input = None;
    let mut output = None;
    let mut args = env::args().skip(1);
    while let Some(argument) = args.next() {
        match argument.as_str() {
            "--input" => input = args.next(),
            "--output" => output = args.next(),
            _ => panic!("unknown argument: {argument}"),
        }
    }

    (
        input.expect("usage: oracle --input cases.json --output results.jsonl"),
        output.expect("usage: oracle --input cases.json --output results.jsonl"),
    )
}
